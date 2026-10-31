using Elsa.Api.Client.Resources.OutputConverters.Contracts;
using Elsa.Api.Client.Resources.OutputConverters.Models;
using Elsa.Api.Client.Resources.OutputConverters.Requests;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// Retrieves output converter descriptors from the connected Elsa server.
/// </summary>
public class RemoteOutputConverterService(IBackendApiClientProvider backendApiClientProvider) : IOutputConverterService
{
    /// <inheritdoc />
    public async Task<ICollection<OutputConverterDescriptor>> GetOutputConvertersAsync(
        string sourceType,
        string destinationType,
        CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IOutputConvertersApi>(cancellationToken);
        var response = await api.ListAsync(new ListOutputConvertersRequest
        {
            SourceType = sourceType,
            DestinationType = destinationType
        }, cancellationToken);

        return response.Items;
    }
}
