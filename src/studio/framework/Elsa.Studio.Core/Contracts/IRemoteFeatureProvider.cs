using Elsa.Api.Client.Resources.Features.Models;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides a way to check if a feature is enabled.
/// </summary>
public interface IRemoteFeatureProvider
{
    /// <summary>
    /// Returns a value indicating whether the feature with the specified name is enabled.
    /// </summary>
    /// <param name="featureName">The name of the feature.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A value indicating whether the feature with the specified name is enabled.</returns>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a list of installed features.
    /// </summary>
    Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default);
}