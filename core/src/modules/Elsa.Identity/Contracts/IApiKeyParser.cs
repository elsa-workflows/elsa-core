namespace Elsa.Identity.Contracts;

/// <summary>
/// Parses an API key into its constituent parts.
/// </summary>
public interface IApiKeyParser
{
    /// <summary>
    /// Parses the specified API key into its constituent parts.
    /// </summary>
    /// <param name="apiKey">The API key to parse.</param>
    /// <returns>The client ID.</returns>
    string Parse(string apiKey);
}