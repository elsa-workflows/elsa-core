namespace Elsa.Secrets;

public class SecretModel
{
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// The logical ID of the secret. All versions share this ID.
    /// </summary>
    public string SecretId { get; set; } = default!;
    
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// An optional scope of the secret. Examples: "API Key", "ConnectionString", "JWT", "Password", "EncryptionKey", etc.
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
    
    public TimeSpan? ExpiresIn { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    
    /// <summary>
    /// The version of the secret. Increments after each update or rotation.
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// The status of the secret.
    /// </summary>
    public SecretStatus Status { get; set; }
    
    public DateTimeOffset? LastAccessedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Owner { get; set; }
}