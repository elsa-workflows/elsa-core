using Elsa.Common.Entities;

namespace Elsa.Secrets.Management;

public class Secret : ManagedEntity
{
    /// <summary>
    /// The logical ID of the secret. All versions share this ID.
    /// </summary>
    public string SecretId { get; set; } = default!;
    
    /// The unique name of the secret.
    public string Name { get; set; } = default!;
    
    /// An optional type of the secret. Examples: "API Key", "ConnectionString", "JWT", "Password", "EncryptionKey", etc.
    public string? Type { get; set; } = default!;
    
    /// The encrypted value of the secret using the encryption key referenced by <see cref="EncryptionKeyId"/>.
    public string EncryptedValue { get; set; } = default!;
    
    /// The Initialization Vector used when encrypting this secret value.
    public string IV { get; set; } = default!;
    
    /// The ID of the encryption key used to encrypt the value.    
    public string EncryptionKeyId { get; set; } = default!;
    
    /// An optional algorithm used for encrypting secrets. If not specified, the algorithm linked with the encryption key ID is used.
    public string? Algorithm { get; set; }
    
    /// An optional description of the secret.
    public string Description { get; set; } = "";
    
    /// The version of the secret. Increments after each update or rotation.
    public int Version { get; set; }
    
    /// The status of the secret.
    public SecretStatus Status { get; set; }
    
    public DateTimeOffset ExpiresAt { get; set; }
    public string? RotationPolicy { get; set; }
    public DateTimeOffset? LastAccessedAt { get; set; }

    public EncryptedValue GetEncryptedValue()
    {
        return new(EncryptedValue, IV, EncryptionKeyId);
    }
}