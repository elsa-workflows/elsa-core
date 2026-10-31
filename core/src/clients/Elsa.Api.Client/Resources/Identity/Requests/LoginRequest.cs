namespace Elsa.Api.Client.Resources.Identity.Requests;

/// <summary>
/// Represents a request to log in.
/// </summary>
/// <param name="Username">The username.</param>
/// <param name="Password">The password.</param>
public record LoginRequest(string Username, string Password);