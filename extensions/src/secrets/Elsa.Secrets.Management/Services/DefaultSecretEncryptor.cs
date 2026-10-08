using Elsa.Common;
using Elsa.Workflows;

namespace Elsa.Secrets.Management;

public class DefaultSecretEncryptor(IEncryptor encryptor, IDecryptor decryptor, IIdentityGenerator identityGenerator, ISystemClock systemClock) : ISecretEncryptor
{
    public async Task<Secret> EncryptAsync(SecretInputModel input, CancellationToken cancellationToken = default)
    {
        var secret = new Secret
        {
            Id = identityGenerator.GenerateId(),
            SecretId = identityGenerator.GenerateId(),
            Version = 1,
            IsLatest = true
        };

        await EncryptAsync(secret, input, cancellationToken);
        return secret;
    }

    public async Task EncryptAsync(Secret secret, SecretInputModel input, CancellationToken cancellationToken = default)
    {
        var encryptedValue = string.IsNullOrWhiteSpace(input.Value) ? "" : await encryptor.EncryptAsync(input.Value, cancellationToken);

        secret.Name = input.Name.Trim();
        secret.Scope = input.Scope?.Trim();
        secret.Description = input.Description.Trim();
        secret.EncryptedValue = encryptedValue;
        secret.ExpiresIn = input.ExpiresIn;
        secret.ExpiresAt = input.ExpiresIn != null ? systemClock.UtcNow + input.ExpiresIn.Value : null;
        secret.Status = SecretStatus.Active;
    }

    public async Task<string> DecryptAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secret.EncryptedValue))
            return "";

        return await decryptor.DecryptAsync(secret.EncryptedValue, cancellationToken);
    }
}