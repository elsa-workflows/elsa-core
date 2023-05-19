namespace Elsa.Api.Client.Resources.Identity.Models;

/// <summary>
/// The response of logging in.
/// </summary>
public record LoginResponse(bool IsAuthenticated, string? AccessToken, string? RefreshToken);