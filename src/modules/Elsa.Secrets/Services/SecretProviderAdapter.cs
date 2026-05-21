using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Elsa.Secrets.Services;

public class SecretProviderAdapter(ISecretResolver resolver, ILogger<SecretProviderAdapter>? logger = null) : ISecretProvider
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
        catch (InvalidOperationException e)
        {
            logger?.LogWarning(e, "Secret '{SecretName}' is unavailable.", name);
            return null;
        }
        catch (CryptographicException e)
        {
            logger?.LogWarning(e, "Secret '{SecretName}' could not be decrypted.", name);
            return null;
        }
        catch (FormatException e)
        {
            logger?.LogWarning(e, "Secret '{SecretName}' has a malformed encrypted payload.", name);
            return null;
        }
    }
}
