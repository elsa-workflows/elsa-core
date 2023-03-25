using Elsa.Common.Entities;

namespace Elsa.Identity.Entities;

/// <summary>
/// Represents a role.
/// </summary>
public class Role : Entity
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the permissions.
    /// </summary>
    public ICollection<string> Permissions { get; set; } = new List<string>();
}