using Elsa.Identity.Entities;

namespace Elsa.Identity.Models;

/// <summary>
/// Result of a user creation operation.
/// </summary>
/// <param name="User">The created user entity.</param>
/// <param name="Password">The plain-text password (either provided or generated).</param>
/// <param name="IsPasswordGenerated">
/// <c>true</c> when the manager generated <paramref name="Password"/> because the caller supplied none.
/// Only a generated password may be surfaced to the caller; a supplied one is already known to them and must never be echoed.
/// </param>
public record CreateUserResult(User User, string Password, bool IsPasswordGenerated = false);
