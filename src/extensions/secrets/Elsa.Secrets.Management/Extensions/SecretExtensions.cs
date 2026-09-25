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
            EncryptedValue = secret.EncryptedValue,
            ExpiresIn = secret.ExpiresIn,
            ExpiresAt = secret.ExpiresAt,
            Status = secret.Status,
            Scope = secret.Scope,
            Version = secret.Version,
            Owner = secret.Owner,
            CreatedAt = secret.CreatedAt,
            UpdatedAt = secret.UpdatedAt,
            LastAccessedAt = secret.LastAccessedAt
        };
    }
    
    public static SecretInputModel ToInputModel(this Secret secret, string clearTextValue)
    {
        return new SecretInputModel
        {
            Name = secret.Name,
            Description = secret.Description,
            Value = clearTextValue,
            ExpiresIn = secret.ExpiresIn,
            Scope = secret.Scope
        };
    }
}