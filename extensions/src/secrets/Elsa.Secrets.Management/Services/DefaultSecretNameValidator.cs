namespace Elsa.Secrets.Management;

public class DefaultSecretNameValidator(ISecretStore store) : ISecretNameValidator
{
    public async Task<bool> IsNameUniqueAsync(string name, string? notId, CancellationToken cancellationToken = default)
    {
        var filter = new SecretFilter
        {
            Name = name,
            NotSecretId = notId
        };
        return await store.FindAsync(filter, cancellationToken) == null;
    }
}