using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.Planning;
using Microsoft.Extensions.Options;

namespace Elsa.ModularPersistence.Runtime;

public sealed class RuntimePhysicalizationOperations(
    IRuntimeStorageDefinitionStore definitionStore,
    IRuntimeStorageDefinitionManager definitionManager,
    IEnumerable<IStoragePhysicalizationPlanner> planners,
    IOptions<ModularPersistenceOptions> options) : IRuntimePhysicalizationOperations
{
    public async ValueTask<IReadOnlyCollection<StoragePhysicalizationPlan>> PlanAsync(string definitionId, string? providerName = null, CancellationToken cancellationToken = default)
    {
        var definition = await GetExistingAsync(definitionId, cancellationToken);
        var manifestResult = RuntimeStorageDefinitionManifestFactory.CreateManifest(definition);
        if (!manifestResult.Succeeded)
            throw new InvalidOperationException($"Runtime storage definition '{definitionId}' is invalid: {string.Join("; ", manifestResult.Errors.Select(x => x.Message))}");

        var selectedProviderName = string.IsNullOrWhiteSpace(providerName) ? options.Value.ProviderName : providerName;
        var matchingPlanners = planners
            .Where(x => string.IsNullOrWhiteSpace(selectedProviderName) || string.Equals(x.ProviderName, selectedProviderName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matchingPlanners.Length == 0)
            throw new InvalidOperationException($"No storage physicalization planner is registered for provider '{selectedProviderName ?? "default"}'.");

        return matchingPlanners.Select(x => x.Plan(manifestResult.Manifest!)).ToArray();
    }

    public async ValueTask<RuntimeStorageDefinition> PromoteIndexAsync(string definitionId, string indexName, RuntimeStorageOperationContext? context = null, CancellationToken cancellationToken = default)
    {
        var definition = await GetDraftAsync(definitionId, cancellationToken);
        var promoted = ReplaceIndex(definition, indexName, PhysicalizationIntent.OptimizedIndexes);
        return await definitionManager.SaveDraftAsync(promoted, context, cancellationToken);
    }

    public async ValueTask<RuntimeStorageDefinition> DemoteIndexAsync(string definitionId, string indexName, RuntimeStorageOperationContext? context = null, CancellationToken cancellationToken = default)
    {
        var definition = await GetDraftAsync(definitionId, cancellationToken);
        var demoted = ReplaceIndex(definition, indexName, PhysicalizationIntent.PortableDocument);
        return await definitionManager.SaveDraftAsync(demoted, context, cancellationToken);
    }

    private async ValueTask<RuntimeStorageDefinition> GetExistingAsync(string definitionId, CancellationToken cancellationToken)
    {
        var definition = await definitionStore.GetAsync(definitionId, cancellationToken);
        return definition ?? throw new KeyNotFoundException($"Runtime storage definition '{definitionId}' was not found.");
    }

    private async ValueTask<RuntimeStorageDefinition> GetDraftAsync(string definitionId, CancellationToken cancellationToken)
    {
        var definition = await GetExistingAsync(definitionId, cancellationToken);
        if (definition.State != RuntimeStorageDefinitionState.Draft)
            throw new InvalidOperationException("Only draft runtime storage definitions can be promoted or demoted. Published definitions must be changed through a new definition version.");

        return definition;
    }

    private static RuntimeStorageDefinition ReplaceIndex(RuntimeStorageDefinition definition, string indexName, PhysicalizationIntent intent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexName);

        var found = false;
        var indexes = definition.Indexes.Select(index =>
        {
            if (!string.Equals(index.Name, indexName, StringComparison.Ordinal))
                return index;

            found = true;
            return new RuntimeStorageIndexDefinition(index.Name, index.FieldNames, index.IsUnique, intent);
        }).ToArray();

        if (!found)
            throw new KeyNotFoundException($"Runtime storage definition '{definition.Id}' does not declare index '{indexName}'.");

        return new RuntimeStorageDefinition(
            definition.Id,
            definition.SchemaName,
            definition.StorageUnitName,
            definition.Fields,
            indexes,
            definition.RequiredPermissions,
            definition.Version)
        {
            State = definition.State
        };
    }
}
