using Elsa.Common.Entities;

namespace Elsa.Secrets.Management.Entities;

public class Secret : ManagedEntity
{
    /// The unique name of the secret.
    public string Name { get; set; } = default!;
    
    /// An optional type of the secret. Examples: "API Key", "ConnectionString", "JWT", "Password", "Encryption Key", etc.
    public string? Type { get; set; } = default!;
    
    /// The encrypted value of the secret using the encryption key referenced by <see cref="EncryptionKeyId"/>.
    public string EncryptedValue { get; set; } = default!;

    /// <summary>
    /// An optional description of the secret.
    /// </summary>
    public string Description { get; set; } = "";

    /// The ID of the encryption key used to encrypt the value. Could be a secret in the same store, or a secret stored in an external KMS.    
    public string EncryptionKeyId { get; set; } = default!;
    
    /// The version of the secret. Increments after each update or rotation.
    public int Version { get; set; }
    
    /// The status of the secret.
    public SecretStatus Status { get; set; }
    
    public DateTimeOffset ExpiresAt { get; set; }
    public string? RotationPolicy { get; set; }
    public DateTimeOffset? LastAccessedAt { get; set; }
}