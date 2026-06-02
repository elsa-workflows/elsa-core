using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.Validation;
using Microsoft.Extensions.Options;

namespace Elsa.ModularPersistence.Runtime;

public sealed class RuntimeStorageDefinitionManager(
    IRuntimeStorageDefinitionStore store,
    IEnumerable<IStorageManifestMaterializer> materializers,
    IStorageProviderCapabilitiesRegistry capabilitiesRegistry,
    IOptions<ModularPersistenceOptions> options,
    IStorageManifestMaterializationTracker materializationTracker) : IRuntimeStorageDefinitionManager
{
    private readonly StorageManifestValidator _validator = new();

    public async ValueTask<RuntimeStorageDefinition> SaveDraftAsync(RuntimeStorageDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.State != RuntimeStorageDefinitionState.Draft)
            throw new InvalidOperationException("Only draft runtime storage definitions can be saved as drafts.");

        var existing = await store.GetAsync(definition.Id, cancellationToken);
        if (existing is not null && existing.State != RuntimeStorageDefinitionState.Draft)
            throw new InvalidOperationException($"Runtime storage definition '{definition.Id}' is {existing.State} and can no longer be saved as a draft.");

        await store.SaveAsync(definition, cancellationToken);
        return definition;
    }

    public ValueTask<RuntimeStorageDefinition?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        store.GetAsync(id, cancellationToken);

    public ValueTask<IReadOnlyCollection<RuntimeStorageDefinition>> ListAsync(CancellationToken cancellationToken = default) =>
        store.ListAsync(cancellationToken);

    public async ValueTask<RuntimeStorageDefinitionPublishResult> PublishAsync(string id, string? providerName = null, CancellationToken cancellationToken = default)
    {
        var definition = await GetExistingAsync(id, cancellationToken);
        if (definition.State == RuntimeStorageDefinitionState.Retired)
            return RuntimeStorageDefinitionPublishResult.Failed(definition, [Error("InvalidLifecycleState", "Retired runtime storage definitions cannot be published.", "state")]);

        var result = await ValidateAndMaterializeAsync(definition, providerName, cancellationToken);
        if (!result.Succeeded)
            return result;

        var published = definition with { State = RuntimeStorageDefinitionState.Published };
        await store.SaveAsync(published, cancellationToken);
        return RuntimeStorageDefinitionPublishResult.Success(published, result.Manifest!);
    }

    public async ValueTask<RuntimeStorageDefinitionPublishResult> RematerializeAsync(string id, string? providerName = null, CancellationToken cancellationToken = default)
    {
        var definition = await GetExistingAsync(id, cancellationToken);
        if (definition.State != RuntimeStorageDefinitionState.Published)
            return RuntimeStorageDefinitionPublishResult.Failed(definition, [Error("InvalidLifecycleState", "Only published runtime storage definitions can be rematerialized.", "state")]);

        return await ValidateAndMaterializeAsync(definition, providerName, cancellationToken);
    }

    public async ValueTask<RuntimeStorageDefinition> RetireAsync(string id, CancellationToken cancellationToken = default)
    {
        var definition = await GetExistingAsync(id, cancellationToken);
        if (definition.State == RuntimeStorageDefinitionState.Draft)
            throw new InvalidOperationException("Draft runtime storage definitions cannot be retired.");

        var retired = definition with { State = RuntimeStorageDefinitionState.Retired };
        await store.SaveAsync(retired, cancellationToken);
        return retired;
    }

    private async ValueTask<RuntimeStorageDefinitionPublishResult> ValidateAndMaterializeAsync(RuntimeStorageDefinition definition, string? providerName, CancellationToken cancellationToken)
    {
        var materializerList = ResolveMaterializers(providerName).ToArray();
        if (materializerList.Length == 0)
            return RuntimeStorageDefinitionPublishResult.Failed(definition, [Error("MissingProvider", "No modular persistence materializer is registered for the selected provider.", "providerName")]);

        var manifestResult = TryCreateManifest(definition);
        if (!manifestResult.Succeeded)
            return manifestResult;

        var manifest = manifestResult.Manifest!;
        var errors = new List<StorageManifestValidationError>();
        foreach (var materializer in materializerList)
        {
            if (!materializer.CanMaterialize(manifest))
            {
                errors.Add(Error("UnsupportedManifest", $"Provider '{materializer.ProviderName}' cannot materialize runtime storage definition '{definition.Id}'.", "providerName"));
                continue;
            }

            var validation = _validator.Validate(manifest, capabilitiesRegistry.GetCapabilities(materializer.ProviderName));
            errors.AddRange(validation.Errors.Select(x => x with { Path = $"providers['{materializer.ProviderName}'].{x.Path}" }));
        }

        if (errors.Count > 0)
            return RuntimeStorageDefinitionPublishResult.Failed(definition, errors);

        foreach (var materializer in materializerList)
        {
            try
            {
                await materializer.MaterializeAsync(manifest, cancellationToken);
                materializationTracker.RecordApplied(materializer.ProviderName, manifest.SchemaName, manifest.Version.ToString(), DateTimeOffset.UtcNow);
            }
            catch (Exception e)
            {
                materializationTracker.RecordFailed(materializer.ProviderName, manifest.SchemaName, manifest.Version.ToString(), DateTimeOffset.UtcNow, e);
                throw;
            }
        }

        return RuntimeStorageDefinitionPublishResult.Success(definition, manifest);
    }

    private IEnumerable<IStorageManifestMaterializer> ResolveMaterializers(string? providerName)
    {
        var selectedProviderName = string.IsNullOrWhiteSpace(providerName) ? options.Value.ProviderName : providerName;
        foreach (var materializer in materializers)
        {
            if (string.IsNullOrWhiteSpace(selectedProviderName) || string.Equals(materializer.ProviderName, selectedProviderName, StringComparison.OrdinalIgnoreCase))
                yield return materializer;
        }
    }

    private RuntimeStorageDefinitionPublishResult TryCreateManifest(RuntimeStorageDefinition definition)
    {
        var errors = ValidateDefinition(definition).ToList();
        if (errors.Count > 0)
            return RuntimeStorageDefinitionPublishResult.Failed(definition, errors);

        try
        {
            var fields = definition.Fields
                .Select(x => new StorageFieldDescriptor(x.Name, x.Type, x.IsRequired))
                .ToArray();
            var indexes = definition.Indexes
                .Select(x => new StorageIndexDescriptor(x.Name, x.FieldNames.Select(fieldName => new StorageIndexFieldDescriptor(fieldName)), x.IsUnique, x.PhysicalizationIntent))
                .ToArray();
            var manifest = new StorageManifestDescriptor(
                definition.SchemaName,
                definition.Version,
                [
                    new StorageUnitDescriptor(
                        definition.StorageUnitName,
                        fields,
                        [new StorageKeyDescriptor($"PK_{definition.StorageUnitName}", ["Id"])],
                        indexes,
                        PhysicalizationIntent.PortableDocument,
                        StorageUnitKind.Document)
                ]);

            return RuntimeStorageDefinitionPublishResult.Success(definition, manifest);
        }
        catch (Exception e) when (e is ArgumentException or ArgumentOutOfRangeException)
        {
            var path = e switch
            {
                ArgumentException argumentException => argumentException.ParamName ?? "definition",
                _ => "definition"
            };
            return RuntimeStorageDefinitionPublishResult.Failed(definition, [Error("InvalidDescriptor", e.Message, path)]);
        }
    }

    private static IEnumerable<StorageManifestValidationError> ValidateDefinition(RuntimeStorageDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.SchemaName))
            yield return Error("MissingSchemaName", "Runtime storage definitions require a schema name.", "schemaName");

        if (string.IsNullOrWhiteSpace(definition.StorageUnitName))
            yield return Error("MissingStorageUnitName", "Runtime storage definitions require a storage unit name.", "storageUnitName");

        if (definition.Fields.Count == 0)
            yield return Error("MissingFields", "Runtime storage definitions require at least one field.", "fields");

        if (definition.Fields.All(x => !string.Equals(x.Name, "Id", StringComparison.Ordinal)))
            yield return Error("MissingIdField", "Runtime storage definitions must declare an Id field for portable document identity.", "fields");

        if (definition.RequiredPermissions.Count == 0)
            yield return Error("MissingPermissionRequirement", "Runtime storage definitions must declare at least one required permission.", "requiredPermissions");

        for (var i = 0; i < definition.RequiredPermissions.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(definition.RequiredPermissions[i]))
                yield return Error("InvalidPermissionRequirement", "Runtime storage definition permission requirements cannot be blank.", $"requiredPermissions[{i}]");
        }
    }

    private async ValueTask<RuntimeStorageDefinition> GetExistingAsync(string id, CancellationToken cancellationToken)
    {
        var definition = await store.GetAsync(id, cancellationToken);
        return definition ?? throw new KeyNotFoundException($"Runtime storage definition '{id}' was not found.");
    }

    private static StorageManifestValidationError Error(string code, string message, string path) =>
        new(code, message, path);
}
