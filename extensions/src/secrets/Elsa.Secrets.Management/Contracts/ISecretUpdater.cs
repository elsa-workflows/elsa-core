namespace Elsa.Secrets.Management;

public interface ISecretUpdater
{
    Task<Secret> UpdateAsync(Secret secret, SecretInputModel input, CancellationToken cancellationToken = default);
}