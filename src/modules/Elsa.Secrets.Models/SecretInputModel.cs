namespace Elsa.Secrets;

public class SecretInputModel
{
    /// <summary>
    /// The unique name of the secret.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// An optional type of the secret. Examples: "API Key", "ConnectionString", "JWT", "Password", "EncryptionKey", etc.
    /// </summary>
    public string? Scope { get; set; } = default!;
    
    /// <summary>
    /// The clear-text value of the secret.
    /// </summary>
    public string Value { get; set; } = default!;
    
    /// <summary>
    /// An optional description of the secret.
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// The expiration date of the secret. Leave null for no expiration.
    /// </summary>
    public TimeSpan? ExpiresIn { get; set; }
}