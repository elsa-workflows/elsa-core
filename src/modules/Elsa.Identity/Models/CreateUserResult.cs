using Elsa.Identity.Entities;

namespace Elsa.Identity.Models;

/// <summary>
/// Result of a user creation operation.
/// </summary>
/// <param name="User">The created user entity.</param>
/// <param name="Password">The plain-text password (either provided or generated).</param>
public record CreateUserResult(User User, string Password);
