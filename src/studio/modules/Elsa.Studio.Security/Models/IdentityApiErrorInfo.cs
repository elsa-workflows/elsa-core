namespace Elsa.Studio.Security.Models;

/// <summary>
/// Safe, display-ready information about a failed Identity request.
/// </summary>
public sealed record IdentityApiErrorInfo(
    string Code,
    string Message,
    bool IsAuthorization = false,
    bool IsNotFound = false,
    bool IsConflict = false,
    bool IsValidation = false)
{
    public static IdentityApiErrorInfo Unavailable { get; } = UnavailableFor("role");

    /// <summary>Builds the generic unavailable error for one administration subject such as <c>role</c> or <c>user</c>.</summary>
    public static IdentityApiErrorInfo UnavailableFor(string subject) =>
        new("unavailable", $"{char.ToUpperInvariant(subject[0])}{subject[1..]} administration is unavailable right now. Try again in a moment.");
}
