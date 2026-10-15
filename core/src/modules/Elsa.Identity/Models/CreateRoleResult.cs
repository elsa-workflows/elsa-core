using Elsa.Identity.Entities;

namespace Elsa.Identity.Models;

/// <summary>
/// Result of a role creation operation.
/// </summary>
/// <param name="Role">The created role entity.</param>
public record CreateRoleResult(Role Role);
