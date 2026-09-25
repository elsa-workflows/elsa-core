using Elsa.Studio.Contracts;

namespace Elsa.Studio.Abstractions;

/// <summary>
/// A base class for modules.
/// </summary>
public abstract class FeatureBase : IFeature
{
    /// <inheritdoc />
    public virtual ValueTask InitializeAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}