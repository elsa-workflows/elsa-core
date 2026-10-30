using Elsa.Identity.Models;

namespace Elsa.Identity.Endpoints.Users.Create;

internal class Request
{
    public string Name { get; set; } = null!;
    public string? Password { get; set; }
    public ICollection<string>? Roles { get; set; }
}

/// <summary>
/// The account as created. Credential material never crosses this boundary: password hashes and salts are omitted,
/// and <see cref="GeneratedPassword"/> is populated only when the server generated the password because the caller
/// supplied none. It is returned exactly once and cannot be retrieved afterwards.
/// </summary>
internal record Response(
    string Id,
    string Name,
    ICollection<string> Roles,
    string? TenantId,
    string? GeneratedPassword)
{
    public static Response FromResult(CreateUserResult result) => new(
        result.User.Id,
        result.User.Name,
        result.User.Roles,
        result.User.TenantId,
        result.IsPasswordGenerated ? result.Password : null);
}
