namespace Elsa.Identity.Models;

/// <summary>
/// The sign-in session a refresh token belongs to.
/// </summary>
/// <param name="Id">The session ID.</param>
/// <param name="ExpiresAt">
/// The latest expiry of the refresh tokens from sign-in up to and including the one the session was read from. That is
/// usually the token's own expiry, but one issued earlier with a longer lifetime expires later, so a refresh token that
/// continues the session carries this forward.
/// </param>
public sealed record SignInSession(string Id, DateTimeOffset ExpiresAt);
