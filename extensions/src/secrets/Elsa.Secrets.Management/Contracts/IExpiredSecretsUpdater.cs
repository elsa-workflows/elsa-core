namespace Elsa.Secrets.Management;

public interface IExpiredSecretsUpdater
{
    Task UpdateExpiredSecretsAsync(CancellationToken cancellationToken = default);
}