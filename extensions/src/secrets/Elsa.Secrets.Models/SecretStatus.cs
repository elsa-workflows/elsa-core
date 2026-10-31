namespace Elsa.Secrets;

public enum SecretStatus
{
    /// <summary>
    /// The secret is active.
    /// </summary>
    Active,
    
    /// <summary>
    /// The secret is retired due to an update which created a new version.
    /// </summary>
    Retired,
    
    /// <summary>
    /// The secret has expired.
    /// </summary>
    Expired,
    
    /// <summary>
    /// The secret has been revoked.
    /// </summary>
    Revoked
}