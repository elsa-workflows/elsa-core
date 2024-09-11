namespace Elsa.Secrets;

public class SecretInputModel
{
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
    
    public DateTimeOffset ExpiresAt { get; set; }
    public string? RotationPolicy { get; set; }
}