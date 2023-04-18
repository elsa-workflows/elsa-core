namespace Elsa.Expressions.Contracts;

/// <summary>
/// Populates the expression syntax registry.
/// </summary>
public interface IExpressionSyntaxRegistryPopulator
{
    /// <summary>
    /// Populates the expression syntax registry.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask PopulateRegistryAsync(CancellationToken cancellationToken = default);
}