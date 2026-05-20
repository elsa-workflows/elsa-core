namespace Elsa.Identity.Options;

/// <summary>
/// Options for the built-in admin API key provider.
/// </summary>
public class AdminApiKeyOptions
{
    /// <summary>
    /// Gets or sets the API key to accept. Leave empty to disable the provider.
    /// </summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Gets or sets the owner name assigned to the API key identity.
    /// </summary>
    public string OwnerName { get; set; } = "admin";

    /// <summary>
    /// Gets or sets the permissions assigned to the API key identity.
    /// </summary>
    public ICollection<string> Permissions { get; set; } = ["*"];
}
