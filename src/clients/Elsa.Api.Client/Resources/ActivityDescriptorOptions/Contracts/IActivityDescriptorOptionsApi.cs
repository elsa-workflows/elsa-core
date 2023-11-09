using Elsa.Api.Client.Resources.ActivityDescriptorOptions.Requests;
using Elsa.Api.Client.Resources.ActivityDescriptorOptions.Responses;
using Refit;

namespace Elsa.Api.Client.Resources.ActivityDescriptorOptions.Contracts;

/// <summary>
/// Represents a client for the workflow definitions API.
/// </summary>
public interface IActivityDescriptorOptionsApi
{
    /// <summary>
    /// Lists activity descriptors options.
    /// </summary>
    /// <param name="typeName">Type Name of the activity</param>
    /// <param name="propertyName"></param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response containing the activity descriptors.</returns>
    [Get("/descriptors/activities/{typeName}/options/{propertyName}")]
    Task<GetActivityDescriptorOptionsResponse> GetAsync(string typeName, string propertyName, CancellationToken cancellationToken = default);
}