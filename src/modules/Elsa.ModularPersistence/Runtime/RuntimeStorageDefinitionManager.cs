using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.Validation;
using Microsoft.Extensions.Options;

namespace Elsa.ModularPersistence.Runtime;

public sealed class RuntimeStorageDefinitionManager(
    IRuntimeStorageDefinitionStore store,
    IEnumerable<IStorageManifestMaterializer> materializers,
    IStorageProviderCapabilitiesRegistry capabilitiesRegistry,
    IOptions<ModularPersistenceOptions> options,
    IStorageManifestMaterializationTracker materializationTracker,
    IRuntimeSchemaAuditTrail auditTrail) : IRuntimeStorageDefinitionManager
{
    private readonly StorageManifestValidator _validator = new();

    public async ValueTask<RuntimeStorageDefinition> SaveDraftAsync(RuntimeStorageDefinition definition, RuntimeStorageOperationContext? context = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.State != RuntimeStorageDefinitionState.Draft)
            throw new InvalidOperationException("Only draft runtime storage definitions can be saved as drafts.");

        var existing = await store.GetAsync(definition.Id, cancellationToken);
        if (existing is not null && existing.State != RuntimeStorageDefinitionState.Draft)
            throw new InvalidOperationException($"Runtime storage definition '{definition.Id}' is {existing.State} and can no longer be saved as a draft.");

        await store.SaveAsync(definition, cancellationToken);
        await AuditAsync(definition.Id, RuntimeSchemaAuditAction.DraftSaved, context, existing, definition, null, true, [], cancellationToken);
        return definition;
    }

    public ValueTask<RuntimeStorageDefinition?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        store.GetAsync(id, cancellationToken);

    public ValueTask<IReadOnlyCollection<RuntimeStorageDefinition>> ListAsync(CancellationToken cancellationToken = default) =>
        store.ListAsync(cancellationToken);

    public async ValueTask<RuntimeStorageDefinitionPublishResult> PublishAsync(string id, string? providerName = null, RuntimeStorageOperationContext? context = null, CancellationToken cancellationToken = default)
    {
        var definition = await GetExistingAsync(id, cancellationToken);
        if (definition.State == RuntimeStorageDefinitionState.Retired)
        {
            var failure = RuntimeStorageDefinitionPublishResult.Failed(definition, [Error("InvalidLifecycleState", "Retired runtime storage definitions cannot be published.", "state")]);
            await AuditAsync(definition.Id, RuntimeSchemaAuditAction.Published, context, definition, definition, providerName, false, DescribeErrors(failure), cancellationToken);
            return failure;
        }

        var result = await ValidateAndMaterializeAsync(definition, providerName, cancellationToken);
        if (!result.Succeeded)
        {
            await AuditAsync(definition.Id, RuntimeSchemaAuditAction.Published, context, definition, definition, providerName, false, DescribeErrors(result), cancellationToken);
            return result;
        }

        var published = definition with { State = RuntimeStorageDefinitionState.Published };
        await store.SaveAsync(published, cancellationToken);
        await AuditAsync(definition.Id, RuntimeSchemaAuditAction.Published, context, definition, published, providerName, true, [$"Materialized {published.SchemaName} {published.Version}"], cancellationToken);
        return RuntimeStorageDefinitionPublishResult.Success(published, result.Manifest!);
    }

    public async ValueTask<RuntimeStorageDefinitionPublishResult> RematerializeAsync(string id, string? providerName = null, RuntimeStorageOperationContext? context = null, CancellationToken cancellationToken = default)
    {
        var definition = await GetExistingAsync(id, cancellationToken);
        if (definition.State != RuntimeStorageDefinitionState.Published)
        {
            var failure = RuntimeStorageDefinitionPublishResult.Failed(definition, [Error("InvalidLifecycleState", "Only published runtime storage definitions can be rematerialized.", "state")]);
            await AuditAsync(definition.Id, RuntimeSchemaAuditAction.Rematerialized, context, definition, definition, providerName, false, DescribeErrors(failure), cancellationToken);
            return failure;
        }

        var result = await ValidateAndMaterializeAsync(definition, providerName, cancellationToken);
        await AuditAsync(definition.Id, RuntimeSchemaAuditAction.Rematerialized, context, definition, definition, providerName, result.Succeeded, result.Succeeded ? [$"Materialized {definition.SchemaName} {definition.Version}"] : DescribeErrors(result), cancellationToken);
        return result;
    }

    public async ValueTask<RuntimeStorageDefinition> RetireAsync(string id, RuntimeStorageOperationContext? context = null, CancellationToken cancellationToken = default)
    {
        var definition = await GetExistingAsync(id, cancellationToken);
        if (definition.State == RuntimeStorageDefinitionState.Draft)
            throw new InvalidOperationException("Draft runtime storage definitions cannot be retired.");

        var retired = definition with { State = RuntimeStorageDefinitionState.Retired };
        await store.SaveAsync(retired, cancellationToken);
        await AuditAsync(definition.Id, RuntimeSchemaAuditAction.Retired, context, definition, retired, null, true, [], cancellationToken);
        return retired;
    }

    private async ValueTask<RuntimeStorageDefinitionPublishResult> ValidateAndMaterializeAsync(RuntimeStorageDefinition definition, string? providerName, CancellationToken cancellationToken)
    {
        var materializerList = ResolveMaterializers(providerName).ToArray();
        if (materializerList.Length == 0)
            return RuntimeStorageDefinitionPublishResult.Failed(definition, [Error("MissingProvider", "No modular persistence materializer is registered for the selected provider.", "providerName")]);

        var manifestResult = RuntimeStorageDefinitionManifestFactory.CreateManifest(definition);
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

    private async ValueTask<RuntimeStorageDefinition> GetExistingAsync(string id, CancellationToken cancellationToken)
    {
        var definition = await store.GetAsync(id, cancellationToken);
        return definition ?? throw new KeyNotFoundException($"Runtime storage definition '{id}' was not found.");
    }

    private static StorageManifestValidationError Error(string code, string message, string path) =>
        new(code, message, path);

    private async ValueTask AuditAsync(
        string definitionId,
        RuntimeSchemaAuditAction action,
        RuntimeStorageOperationContext? context,
        RuntimeStorageDefinition? before,
        RuntimeStorageDefinition? after,
        string? providerName,
        bool succeeded,
        IReadOnlyCollection<string> providerResults,
        CancellationToken cancellationToken)
    {
        var entry = new RuntimeSchemaAuditEntry(
            Guid.NewGuid().ToString("N"),
            definitionId,
            action,
            (context ?? RuntimeStorageOperationContext.System).Actor,
            DateTimeOffset.UtcNow,
            before,
            after,
            providerName ?? options.Value.ProviderName,
            succeeded,
            providerResults);
        await auditTrail.AddAsync(entry, cancellationToken);
    }

    private static IReadOnlyCollection<string> DescribeErrors(RuntimeStorageDefinitionPublishResult result) =>
        result.Errors.Select(x => $"{x.Code}: {x.Message}").ToArray();
}
