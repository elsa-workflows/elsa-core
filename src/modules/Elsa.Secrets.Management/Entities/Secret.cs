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
    
    /// An scope type of the secret. Examples: "API Key", "ConnectionString", "JWT", "Password", "EncryptionKey", etc.
    public string? Scope { get; set; } = default!;
    
    /// The encrypted value of the secret using the encryption key referenced by <see cref="EncryptionKeyId"/>.
    public string EncryptedValue { get; set; } = default!;
    
    /// An optional description of the secret.
    public string Description { get; set; } = "";
    
    /// The version of the secret. Increments after each update or rotation.
    public int Version { get; set; }
    
    /// The status of the secret.
    public SecretStatus Status { get; set; }
    
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastAccessedAt { get; set; }
}