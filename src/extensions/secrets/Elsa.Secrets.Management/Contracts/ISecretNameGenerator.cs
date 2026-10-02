namespace Elsa.Secrets.Management;

public interface ISecretNameGenerator
{
    Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken = default);
}