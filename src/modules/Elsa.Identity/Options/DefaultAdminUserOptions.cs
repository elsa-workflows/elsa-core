namespace Elsa.Identity.Options;

/// <summary>
/// Represents configuration options for a default admin user, including username, password, role name, and role permissions.
/// </summary>
public class DefaultAdminUserOptions
{
    /// <summary>
    /// Gets or sets the admin user name.
    /// </summary>
    public string AdminUserName { get; set; } = "admin";
    
    /// <summary>
    /// Gets or sets the admin user password.
    /// </summary>
    public string AdminPassword { get; set; } = "password";
    
    /// <summary>
    /// Gets or sets the admin role name.
    /// </summary>
    public string AdminRoleName { get; set; } = "admin";
    
    /// <summary>
    /// Gets or sets the admin role permissions.
    /// </summary>
    public ICollection<string> AdminRolePermissions { get; set; } = ["*"];
}