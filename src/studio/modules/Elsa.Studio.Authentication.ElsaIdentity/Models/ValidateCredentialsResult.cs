namespace Elsa.Studio.Authentication.ElsaIdentity.Models;

/// <summary>
/// Result of validating credentials.
/// </summary>
public record ValidateCredentialsResult(bool IsValid, string? AccessToken, string? RefreshToken);

