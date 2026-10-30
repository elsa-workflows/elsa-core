using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Persistence.VNext.Document;
using Elsa.Persistence.VNext.Runtime.Contracts;
using Elsa.Persistence.VNext.Runtime.Models;

namespace Elsa.Persistence.VNext.Runtime.Services;

public class RuntimeEntityManager(IDocumentStore documentStore, RuntimeEntityDefinitionValidator validator) : IRuntimeEntityManager
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<RuntimeEntityDefinition> SaveDraftAsync(RuntimeEntityDefinition definition, CancellationToken cancellationToken = default)
    {
        definition.Name = NormalizeName(definition.Name);
        definition.Status = RuntimeEntityDefinitionStatus.Draft;
        validator.Validate(definition);

        var existing = await LoadDefinitionDocumentAsync(definition.Name, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        definition.CreatedAt = existing?.Definition.CreatedAt ?? definition.CreatedAt;
        definition.UpdatedAt = now;

        await SaveDefinitionAsync(definition, existing?.Document.Version ?? 0, cancellationToken);
        await AppendAuditAsync("RuntimeEntityDefinition", definition.Name, "DraftSaved", $"Draft version {definition.Version} saved.", cancellationToken);
        return definition;
    }

    public async Task<RuntimeEntityDefinition?> GetDefinitionAsync(string name, CancellationToken cancellationToken = default)
    {
        var result = await LoadDefinitionDocumentAsync(name, cancellationToken);
        return result?.Definition;
    }

    public async Task<RuntimeEntityDefinition> PublishAsync(string name, CancellationToken cancellationToken = default)
    {
        var existing = await LoadDefinitionDocumentAsync(name, cancellationToken)
            ?? throw new InvalidOperationException($"Runtime entity definition '{name}' was not found.");

        existing.Definition.Status = RuntimeEntityDefinitionStatus.Published;
        existing.Definition.UpdatedAt = DateTimeOffset.UtcNow;
        validator.Validate(existing.Definition);

        await SaveDefinitionAsync(existing.Definition, existing.Document.Version, cancellationToken);
        await AppendAuditAsync("RuntimeEntityDefinition", existing.Definition.Name, "Published", $"Version {existing.Definition.Version} published.", cancellationToken);
        return existing.Definition;
    }

    public async Task<RuntimeEntityDefinition> RetireAsync(string name, CancellationToken cancellationToken = default)
    {
        var existing = await LoadDefinitionDocumentAsync(name, cancellationToken)
            ?? throw new InvalidOperationException($"Runtime entity definition '{name}' was not found.");

        existing.Definition.Status = RuntimeEntityDefinitionStatus.Retired;
        existing.Definition.UpdatedAt = DateTimeOffset.UtcNow;

        await SaveDefinitionAsync(existing.Definition, existing.Document.Version, cancellationToken);
        await AppendAuditAsync("RuntimeEntityDefinition", existing.Definition.Name, "Retired", $"Version {existing.Definition.Version} retired.", cancellationToken);
        return existing.Definition;
    }

    public async Task<RuntimeEntityInstance> SaveInstanceAsync(RuntimeEntityInstance instance, CancellationToken cancellationToken = default)
    {
        instance.DefinitionName = NormalizeName(instance.DefinitionName);
        var definition = await GetDefinitionAsync(instance.DefinitionName, cancellationToken)
            ?? throw new InvalidOperationException($"Runtime entity definition '{instance.DefinitionName}' was not found.");

        if (definition.Status != RuntimeEntityDefinitionStatus.Published)
            throw new InvalidOperationException($"Runtime entity definition '{definition.Name}' is not published.");

        validator.ValidateInstance(definition, instance);
        instance.DefinitionVersion = definition.Version;

        var existing = await LoadInstanceDocumentAsync(instance.DefinitionName, instance.Id, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        instance.CreatedAt = existing?.Instance.CreatedAt ?? instance.CreatedAt;
        instance.UpdatedAt = now;

        var request = new SaveDocumentRequest(
            RuntimeEntityPersistenceSchemaProvider.InstancesStorageUnit,
            CreateInstanceDocumentId(instance.DefinitionName, instance.Id),
            JsonSerializer.Serialize(instance, _jsonOptions),
            CreateInstanceIndexValues(definition, instance),
            existing?.Document.Version ?? 0);

        await documentStore.SaveAsync(request, cancellationToken);
        await AppendAuditAsync("RuntimeEntityInstance", CreateInstanceDocumentId(instance.DefinitionName, instance.Id), existing is null ? "Created" : "Updated", null, cancellationToken);
        return instance;
    }

    public async Task<RuntimeEntityInstance?> GetInstanceAsync(string definitionName, string id, CancellationToken cancellationToken = default)
    {
        var result = await LoadInstanceDocumentAsync(definitionName, id, cancellationToken);
        return result?.Instance;
    }

    public async Task<bool> DeleteInstanceAsync(string definitionName, string id, CancellationToken cancellationToken = default)
    {
        var documentId = CreateInstanceDocumentId(definitionName, id);
        var deleted = await documentStore.DeleteAsync(RuntimeEntityPersistenceSchemaProvider.InstancesStorageUnit, documentId, cancellationToken: cancellationToken);
        if (deleted)
            await AppendAuditAsync("RuntimeEntityInstance", documentId, "Deleted", null, cancellationToken);

        return deleted;
    }

    public async Task<IReadOnlyList<RuntimeEntityInstance>> QueryInstancesAsync(string definitionName, string indexedFieldName, object? value, CancellationToken cancellationToken = default)
    {
        var normalizedDefinitionName = NormalizeName(definitionName);
        var definition = await GetDefinitionAsync(normalizedDefinitionName, cancellationToken)
            ?? throw new InvalidOperationException($"Runtime entity definition '{definitionName}' was not found.");

        if (!definition.Indexes.Any(x => string.Equals(x.FieldName, indexedFieldName, StringComparison.OrdinalIgnoreCase)))
            throw new DocumentQueryNotIndexedException(RuntimeEntityPersistenceSchemaProvider.InstancesStorageUnit, [indexedFieldName]);

        var results = new List<RuntimeEntityInstance>();
        for (var slot = 1; slot <= RuntimeEntityPersistenceSchemaProvider.IndexedFieldSlotCount; slot++)
        {
            var documents = await documentStore.QueryAsync(new DocumentQuery(
                RuntimeEntityPersistenceSchemaProvider.InstancesStorageUnit,
                new Dictionary<string, string?>
                {
                    [nameof(RuntimeEntityInstance.DefinitionName)] = normalizedDefinitionName,
                    [$"Index{slot}Name"] = indexedFieldName,
                    [$"Index{slot}Value"] = ConvertIndexValue(value)
                }), cancellationToken);

            results.AddRange(documents.Select(DeserializeInstance));
        }

        return results
            .GroupBy(x => x.Id, StringComparer.Ordinal)
            .Select(x => x.First())
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<IReadOnlyList<RuntimeEntityAuditRecord>> ListAuditAsync(string subjectId, CancellationToken cancellationToken = default)
    {
        var documents = await documentStore.QueryAsync(new DocumentQuery(
            RuntimeEntityPersistenceSchemaProvider.AuditStorageUnit,
            new Dictionary<string, string?>
            {
                [nameof(RuntimeEntityAuditRecord.SubjectId)] = subjectId
            }), cancellationToken);

        return documents.Select(DeserializeAudit).OrderBy(x => x.Timestamp).ToList();
    }

    private async Task SaveDefinitionAsync(RuntimeEntityDefinition definition, long expectedVersion, CancellationToken cancellationToken)
    {
        var request = new SaveDocumentRequest(
            RuntimeEntityPersistenceSchemaProvider.DefinitionsStorageUnit,
            NormalizeName(definition.Name),
            JsonSerializer.Serialize(definition, _jsonOptions),
            new Dictionary<string, string?>
            {
                [nameof(RuntimeEntityDefinition.Name)] = definition.Name,
                [nameof(RuntimeEntityDefinition.Status)] = definition.Status.ToString()
            },
            expectedVersion);

        await documentStore.SaveAsync(request, cancellationToken);
    }

    private async Task<(StoredDocument Document, RuntimeEntityDefinition Definition)?> LoadDefinitionDocumentAsync(string name, CancellationToken cancellationToken)
    {
        var document = await documentStore.LoadAsync(RuntimeEntityPersistenceSchemaProvider.DefinitionsStorageUnit, NormalizeName(name), cancellationToken);
        return document is null ? null : (document, DeserializeDefinition(document));
    }

    private async Task<(StoredDocument Document, RuntimeEntityInstance Instance)?> LoadInstanceDocumentAsync(string definitionName, string id, CancellationToken cancellationToken)
    {
        var document = await documentStore.LoadAsync(RuntimeEntityPersistenceSchemaProvider.InstancesStorageUnit, CreateInstanceDocumentId(definitionName, id), cancellationToken);
        return document is null ? null : (document, DeserializeInstance(document));
    }

    private async Task AppendAuditAsync(string subjectType, string subjectId, string action, string? message, CancellationToken cancellationToken)
    {
        var record = new RuntimeEntityAuditRecord
        {
            SubjectType = subjectType,
            SubjectId = subjectId,
            Action = action,
            Message = message
        };

        await documentStore.SaveAsync(new SaveDocumentRequest(
            RuntimeEntityPersistenceSchemaProvider.AuditStorageUnit,
            record.Id,
            JsonSerializer.Serialize(record, _jsonOptions),
            new Dictionary<string, string?>
            {
                [nameof(RuntimeEntityAuditRecord.SubjectType)] = record.SubjectType,
                [nameof(RuntimeEntityAuditRecord.SubjectId)] = record.SubjectId
            },
            0), cancellationToken);
    }

    private Dictionary<string, string?> CreateInstanceIndexValues(RuntimeEntityDefinition definition, RuntimeEntityInstance instance)
    {
        var indexValues = new Dictionary<string, string?>
        {
            [nameof(RuntimeEntityInstance.DefinitionName)] = instance.DefinitionName
        };

        for (var slot = 1; slot <= RuntimeEntityPersistenceSchemaProvider.IndexedFieldSlotCount; slot++)
        {
            indexValues[$"Index{slot}Name"] = string.Empty;
            indexValues[$"Index{slot}Value"] = null;
        }

        for (var index = 0; index < definition.Indexes.Count; index++)
        {
            var indexedField = definition.Indexes[index].FieldName;
            instance.Data.TryGetValue(indexedField, out var value);
            var slot = index + 1;
            indexValues[$"Index{slot}Name"] = indexedField;
            indexValues[$"Index{slot}Value"] = ConvertIndexValue(value);
        }

        return indexValues;
    }

    private RuntimeEntityDefinition DeserializeDefinition(StoredDocument document)
    {
        return JsonSerializer.Deserialize<RuntimeEntityDefinition>(document.Content, _jsonOptions)
            ?? throw new DocumentStoreValidationException($"Runtime entity definition document '{document.Id}' could not be deserialized.");
    }

    private RuntimeEntityInstance DeserializeInstance(StoredDocument document)
    {
        return JsonSerializer.Deserialize<RuntimeEntityInstance>(document.Content, _jsonOptions)
            ?? throw new DocumentStoreValidationException($"Runtime entity instance document '{document.Id}' could not be deserialized.");
    }

    private RuntimeEntityAuditRecord DeserializeAudit(StoredDocument document)
    {
        return JsonSerializer.Deserialize<RuntimeEntityAuditRecord>(document.Content, _jsonOptions)
            ?? throw new DocumentStoreValidationException($"Runtime entity audit document '{document.Id}' could not be deserialized.");
    }

    private static string NormalizeName(string name) => name.Trim().ToLowerInvariant();
    private static string CreateInstanceDocumentId(string definitionName, string id) => $"{NormalizeName(definitionName)}:{id}";

    private static string? ConvertIndexValue(object? value)
    {
        return value switch
        {
            null => null,
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }
}
