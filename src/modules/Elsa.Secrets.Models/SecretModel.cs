namespace Elsa.Secrets;

public class SecretModel : SecretInputModel
{
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// The logical ID of the secret. All versions share this ID.
    /// </summary>
    public string SecretId { get; set; } = default!;
    
    /// The version of the secret. Increments after each update or rotation.
    public int Version { get; set; }
    
    /// The status of the secret.
    public SecretStatus Status { get; set; }
    
    public DateTimeOffset? LastAccessedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Owner { get; set; }
}