using Elsa.Common.Entities;

namespace Elsa.Secrets.Management.Entities;

public class EncryptionKey : ManagedEntity
{
    /// A user friendly alias of the key.
    public string Alias { get; set; } = default!;
    
    /// The encrypted value of the secret using the encryption key referenced by <see cref="EncryptionKeyId"/>.
    public string EncryptedValue { get; set; } = default!;

    /// <summary>
    /// An optional description of the secret.
    /// </summary>
    public string Description { get; set; } = "";

    /// The ID of the "root" encryption key used to encrypt the value, typically stored externally, such as a KMS.    
    public string EncryptionKeyId { get; set; } = default!;
    
    /// The version of the secret. Increments after each update or rotation.
    public int Version { get; set; }
    
    /// The status of the encryption key.
    public SecretStatus Status { get; set; }
    
    public string? Algorithm { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? RotationPolicy { get; set; }
    public DateTimeOffset? LastAccessedAt { get; set; }
}