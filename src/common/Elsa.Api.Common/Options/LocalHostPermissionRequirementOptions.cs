namespace Elsa.Options;

/// <summary>
/// Options for the localhost permission requirement.
/// </summary>
public class LocalHostPermissionRequirementOptions
{
    /// <summary>
    /// Gets or sets whether localhost requests may satisfy the security-root permission requirement without other credentials.
    /// </summary>
    public bool EnableLocalHostPermissionGrant { get; set; }
}
