namespace Elsa.Studio.Login.Contracts;

/// <summary>
/// Manages PKCE (Proof Key for Code Exchange) state for the authorization code flow.
/// </summary>
public interface IOpenIdConnectPkceStateService
{
    /// <summary>
    /// Generates and returns a PKCE code challedge. The associated code verifier is stored in session storage.
    /// </summary>
    Task<(string CodeChallenge, string Method)> GeneratePkceCodeChallenge();

    /// <summary>
    /// Retrieves the code verifier for the current session.
    /// </summary>
    Task<string> GetPkceCodeVerifier();
}