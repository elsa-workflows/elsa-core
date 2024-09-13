using Elsa.Workflows.Contracts;

namespace Elsa.Secrets.Management;

public class DefaultSecretEncryptor(IEncryptor encryptor, IDecryptor decryptor, IIdentityGenerator identityGenerator) : ISecretEncryptor
{
    public async Task<Secret> EncryptAsync(SecretInputModel input, CancellationToken cancellationToken = default)
    {
        var encryptedValue = string.IsNullOrWhiteSpace(input.Value) ? "" : await encryptor.EncryptAsync(input.Value, cancellationToken);
        
        var secret = new Secret
        {
            Id = identityGenerator.GenerateId(),
            SecretId = identityGenerator.GenerateId(),
            Version = 1,
            IsLatest = true,
            Name = input.Name.Trim(),
            Scope = input.Scope?.Trim(),
            Description = input.Description.Trim(),
            EncryptedValue = encryptedValue,
            ExpiresAt = input.ExpiresAt,
            Status = SecretStatus.Active
        };
        
        return secret;
    }

    public async Task EncryptAsync(Secret secret, SecretInputModel input, CancellationToken cancellationToken = default)
    {
        var encryptedValue = string.IsNullOrWhiteSpace(input.Value) ? "" : await encryptor.EncryptAsync(input.Value, cancellationToken);
        
        secret.Name = input.Name.Trim();
        secret.Scope = input.Scope?.Trim();
        secret.Description = input.Description.Trim();
        secret.EncryptedValue = encryptedValue;
        secret.ExpiresAt = input.ExpiresAt;
        secret.Status = SecretStatus.Active;
    }

    public async Task<string> DecryptAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(secret.EncryptedValue))
            return "";
        
        return await decryptor.DecryptAsync(secret.EncryptedValue, cancellationToken);
    }
}