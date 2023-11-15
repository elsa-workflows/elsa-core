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
    /// <param name="activityTypeName">The TypeName of the activity </param>
    /// <param name="propertyName">The name of the property</param>
    /// <param name="request">The context request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response containing the activity descriptors.</returns>
    [Post("/descriptors/activities/{activityTypeName}/options/{propertyName}")]
    Task<GetActivityDescriptorOptionsResponse> GetAsync(string activityTypeName, string propertyName, [Body]GetActivityDescriptorOptionsRequest request, CancellationToken cancellationToken = default);
}