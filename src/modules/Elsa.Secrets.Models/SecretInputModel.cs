namespace Elsa.Secrets;

public class SecretInputModel
{
    /// The unique name of the secret.
    public string Name { get; set; } = default!;
    
    /// An optional type of the secret. Examples: "API Key", "ConnectionString", "JWT", "Password", "EncryptionKey", etc.
    public string? Scope { get; set; } = default!;
    
    /// The clear-text value of the secret.
    public string Value { get; set; } = default!;
    
    /// An optional description of the secret.
    public string Description { get; set; } = "";
    
    /// <summary>
    /// The expiration date of the secret. Leave null for no expiration.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}