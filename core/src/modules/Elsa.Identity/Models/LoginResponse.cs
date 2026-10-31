namespace Elsa.Identity.Models;

public class LoginResponse(bool isAuthenticated, string? accessToken, string? refreshToken)
{
    public bool IsAuthenticated { get; } = isAuthenticated;
    public string? AccessToken { get; } = accessToken;
    public string? RefreshToken { get; } = refreshToken;
}