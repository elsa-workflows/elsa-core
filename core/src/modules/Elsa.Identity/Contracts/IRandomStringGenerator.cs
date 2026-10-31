namespace Elsa.Identity.Contracts;

/// <summary>
/// Represents a generator of random strings.
/// </summary>
public interface IRandomStringGenerator
{
    /// <summary>
    /// Generates a short identifier.
    /// </summary>
    /// <returns>The client ID.</returns>
    string Generate(int length = 32, char[]? chars = null);
}