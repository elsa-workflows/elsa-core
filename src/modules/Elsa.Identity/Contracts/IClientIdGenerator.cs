namespace Elsa.Identity.Contracts;

/// <summary>
/// Represents a generator of short identifiers.
/// </summary>
public interface IClientIdGenerator
{
    /// <summary>
    /// Generates a short identifier.
    /// </summary>
    /// <returns>The short identifier.</returns>
    string Generate();
}