namespace Elsa.Identity.Contracts;

/// <summary>
/// Represents a generator of client identifiers.
/// </summary>
public interface IClientIdGenerator
{
    /// <summary>
    /// Generates a short identifier.
    /// </summary>
    /// <returns>The client ID.</returns>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}