using System.Security.Cryptography;

namespace Elsa.Secrets.Services;

public class SecretProviderAdapter(ISecretResolver resolver) : ISecretProvider
{
    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            return await resolver.ResolveAsync(name, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
