using System.Collections.Concurrent;

namespace Elsa.ModularPersistence.Runtime;

public sealed class InMemoryRuntimeStorageDefinitionStore : IRuntimeStorageDefinitionStore
{
    private readonly ConcurrentDictionary<string, RuntimeStorageDefinition> _definitions = new(StringComparer.Ordinal);

    public ValueTask<RuntimeStorageDefinition?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_definitions.GetValueOrDefault(id));

    public ValueTask<IReadOnlyCollection<RuntimeStorageDefinition>> ListAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<IReadOnlyCollection<RuntimeStorageDefinition>>(_definitions.Values.OrderBy(x => x.SchemaName).ThenBy(x => x.StorageUnitName).ToArray());

    public ValueTask SaveAsync(RuntimeStorageDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        _definitions[definition.Id] = definition;
        return ValueTask.CompletedTask;
    }
}
