namespace Elsa.Secrets.Services;

public class SecretStoreRegistry(IEnumerable<ISecretStore> stores) : ISecretStoreRegistry
{
    private readonly Lazy<Dictionary<string, ISecretStore>> _stores = new(() => stores.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase));

    public IReadOnlyCollection<ISecretStore> List() => _stores.Value.Values.ToList();

    public ISecretStore Get(string name)
    {
        if (TryGet(name, out var store))
            return store!;

        throw new InvalidOperationException($"Secret store '{name}' is not registered.");
    }

    public bool TryGet(string name, out ISecretStore? store) => _stores.Value.TryGetValue(name, out store);
}
