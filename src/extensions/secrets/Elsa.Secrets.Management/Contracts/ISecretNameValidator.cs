namespace Elsa.Secrets.Management;

public interface ISecretNameValidator
{
    Task<bool> IsNameUniqueAsync(string name, string? notId, CancellationToken cancellationToken = default);
}