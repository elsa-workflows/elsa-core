using Elsa.Secrets.Management;

// ReSharper disable once CheckNamespace
namespace Elsa.Secrets;

public static class SecretExtensions
{
    public static SecretModel ToModel(this Secret secret)
    {
        return new SecretModel
        {
            Id = secret.Id,
            SecretId = secret.SecretId,
            Name = secret.Name,
            Description = secret.Description,
            Algorithm = secret.Algorithm,
            EncryptedValue = secret.EncryptedValue,
            IV = secret.IV,
            EncryptionKeyId = secret.EncryptionKeyId,
            ExpiresAt = secret.ExpiresAt,
            RotationPolicy = secret.RotationPolicy,
            Status = secret.Status,
            Type = secret.Type,
            Version = secret.Version,
            Owner = secret.Owner,
            CreatedAt = secret.CreatedAt,
            UpdatedAt = secret.UpdatedAt,
            LastAccessedAt = secret.LastAccessedAt
        };
    }
}