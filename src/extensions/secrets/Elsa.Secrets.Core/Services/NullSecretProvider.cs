using Elsa.Secrets.Contracts;

namespace Elsa.Secrets;

public class NullSecretProvider : ISecretProvider
{
    public Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
}