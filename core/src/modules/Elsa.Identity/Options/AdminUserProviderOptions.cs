namespace Elsa.Identity.Options;

/// <summary>
/// Options for the built-in admin user provider.
/// </summary>
public class AdminUserProviderOptions
{
    /// <summary>
    /// Gets or sets the user ID assigned to the configured admin user.
    /// </summary>
    public string UserId { get; set; } = "admin";

    /// <summary>
    /// Gets or sets the admin user name to accept. Leave empty to disable the provider.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// Gets or sets the admin password to accept. Leave empty to disable the provider.
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// Gets or sets the roles assigned to the configured admin user.
    /// </summary>
    public ICollection<string> Roles { get; set; } = ["admin"];
}
