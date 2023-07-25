using Elsa.Api.Client.Resources.Features.Models;
using Refit;

namespace Elsa.Api.Client.Resources.Features.Contracts;

/// <summary>
/// A client for working with features.
/// </summary>
public interface IFeaturesApi
{
    /// <summary>
    /// Gets the specified feature.
    /// </summary>
    /// <param name="fullName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The feature.</returns>
    [Get("/features/installed/{fullName}")]
    Task<FeatureDescriptor> GetAsync(string fullName, CancellationToken cancellationToken = default);
}