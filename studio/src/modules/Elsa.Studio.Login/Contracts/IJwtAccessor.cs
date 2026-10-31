namespace Elsa.Studio.Login.Contracts;

/// <summary>
/// Reads the token from storage (e.g. cookie, local storage, etc.).
/// </summary>
public interface IJwtAccessor
{
    /// <summary>
    /// Reads the token from storage (e.g. cookie, local storage, etc.).
    /// </summary>
    ValueTask<string?> ReadTokenAsync(string name);
    
    /// <summary>
    /// Writes the token to storage (e.g. cookie, local storage, etc.).
    /// </summary>
    ValueTask WriteTokenAsync(string name, string token);
}