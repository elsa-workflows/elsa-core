namespace Elsa.OrchardCore.Client;

/// <summary>
/// 
/// </summary>
/// <param name="AccessToken"></param>
/// <param name="TokenType"></param>
/// <param name="ExpiresIn"></param>
public record SecurityToken(string AccessToken, string TokenType, int ExpiresIn);