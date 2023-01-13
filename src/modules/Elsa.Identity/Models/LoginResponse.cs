namespace Elsa.Identity.Models;

public class LoginResponse
{
    public LoginResponse(bool isAuthenticated, string? accessToken, string? refreshToken)
    {
        IsAuthenticated = isAuthenticated;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }

    public bool IsAuthenticated { get; }
    public string? AccessToken { get; }
    public string? RefreshToken { get; }
}