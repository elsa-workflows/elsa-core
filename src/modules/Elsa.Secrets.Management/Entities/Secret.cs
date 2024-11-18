using Elsa.Common.Entities;

namespace Elsa.Secrets.Management;

public class Secret : ManagedEntity
{
    /// <summary>
    /// The logical ID of the secret. All versions share this ID.
    /// </summary>
    public string SecretId { get; set; } = default!;
    
    /// <summary>
    /// The unique name of the secret.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// An scope type of the secret. Examples: "API Key", "ConnectionString", "JWT", "Password", "EncryptionKey", etc.
    /// </summary>
    public string? Scope { get; set; } = default!;
    
    /// <summary>
    /// The encrypted value of the secret using the encryption key referenced by <see cref="EncryptionKeyId"/>.
    /// </summary>
    public string EncryptedValue { get; set; } = default!;
    
    /// <summary>
    /// An optional description of the secret.
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// The version of the secret. Increments after each update or rotation.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Whether this secret is the latest version.
    /// </summary>
    public bool IsLatest { get; set; }
    
    /// <summary>
    /// The status of the secret.
    /// </summary>
    public SecretStatus Status { get; set; }

    public TimeSpan? ExpiresIn { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastAccessedAt { get; set; }

    public Secret Clone()
    {
        return new Secret
        {
            Id = Id,
            SecretId = SecretId,
            Name = Name,
            Scope = Scope,
            EncryptedValue = EncryptedValue,
            Description = Description,
            Version = Version,
            IsLatest = IsLatest,
            Status = Status,
            ExpiresIn = ExpiresIn,
            ExpiresAt = ExpiresAt,
            LastAccessedAt = LastAccessedAt,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            Owner = Owner
        };
    }
}