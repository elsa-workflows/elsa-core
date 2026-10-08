using Elsa.Api.Client.Resources.OutputConverters.Requests;
using Elsa.Api.Client.Resources.OutputConverters.Responses;
using Refit;

namespace Elsa.Api.Client.Resources.OutputConverters.Contracts;

/// <summary>
/// Represents a client for output converter descriptor discovery.
/// </summary>
public interface IOutputConvertersApi
{
    /// <summary>
    /// Lists output converters compatible with the specified declared source and destination types.
    /// </summary>
    /// <param name="request">The request containing the declared source and destination type names.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compatible output converter descriptors.</returns>
    [Get("/descriptors/output-converters")]
    Task<ListOutputConvertersResponse> ListAsync([Query] ListOutputConvertersRequest request, CancellationToken cancellationToken = default);
}
