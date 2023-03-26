using Elsa.Identity.Entities;

namespace Elsa.Identity.Options;

/// <summary>
/// Represents options that stores available roles. 
/// </summary>
public class RolesOptions
{
    /// <summary>
    /// Gets or sets the roles.
    /// </summary>
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}