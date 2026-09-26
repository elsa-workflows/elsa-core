namespace Elsa.Studio.Login.Models;

/// <summary>
/// Represents the result of validating user credentials.
/// </summary>
/// <param name="IsAuthenticated">Indicates whether the credentials are valid.</param>
/// <param name="AccessToken">The access token if authentication was successful.</param>
/// <param name="RefreshToken">The refresh token if authentication was successful.</param>
public record ValidateCredentialsResult(bool IsAuthenticated, string? AccessToken, string? RefreshToken);