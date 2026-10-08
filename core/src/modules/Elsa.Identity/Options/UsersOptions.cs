using Elsa.Identity.Entities;

namespace Elsa.Identity.Options;

/// <summary>
/// Represents options that stores available users.
/// </summary>
public class UsersOptions
{
    /// <summary>
    /// Gets or sets the users.
    /// </summary>
    public ICollection<User> Users { get; set; } = new List<User>();
}