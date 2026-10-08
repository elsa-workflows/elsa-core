namespace Elsa.Identity.Contracts;

/// <summary>
/// Represents an API key generator.
/// </summary>
public interface IApiKeyGenerator
{
    /// <summary>
    /// Generates a new API key for the specified application name.
    /// </summary>
    /// <param name="clientId">The name of the application.</param>
    /// <returns>The API key.</returns>
    string Generate(string clientId);
}