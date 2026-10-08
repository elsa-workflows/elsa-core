namespace Elsa.Api.Client.Resources.Identity.Responses;

/// <summary>
/// The response of logging in.
/// </summary>
public record LoginResponse(bool IsAuthenticated, string? AccessToken, string? RefreshToken);