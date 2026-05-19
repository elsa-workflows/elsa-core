namespace Elsa.Secrets.Services;

public class SecretTypeRegistry(IEnumerable<ISecretTypeProvider> providers) : ISecretTypeRegistry
{
    private readonly Lazy<Dictionary<string, ISecretTypeProvider>> _providers = new(() => providers.ToDictionary(x => x.Descriptor.Name, StringComparer.OrdinalIgnoreCase));

    public IReadOnlyCollection<SecretTypeDescriptor> List() => _providers.Value.Values.Select(x => x.Descriptor).ToList();

    public ISecretTypeProvider Get(string name)
    {
        if (TryGet(name, out var provider))
            return provider!;

        throw new InvalidOperationException($"Secret type '{name}' is not registered.");
    }

    public bool TryGet(string name, out ISecretTypeProvider? provider) => _providers.Value.TryGetValue(name, out provider);
}
