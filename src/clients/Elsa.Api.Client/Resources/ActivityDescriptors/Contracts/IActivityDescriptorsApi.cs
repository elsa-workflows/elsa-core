using Elsa.Api.Client.Resources.ActivityDescriptors.Requests;
using Elsa.Api.Client.Resources.ActivityDescriptors.Responses;
using Refit;

namespace Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;

/// <summary>
/// Represents a client for the workflow definitions API.
/// </summary>
public interface IActivityDescriptorsApi
{
    /// <summary>
    /// Lists activity descriptors.
    /// </summary>
    /// <param name="request">The request containing options for listing activity descriptors.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response containing the activity descriptors.</returns>
    [Get("/descriptors/activities")]
    Task<ListActivityDescriptorsResponse> ListAsync([Query]ListActivityDescriptorsRequest request, CancellationToken cancellationToken = default);
}