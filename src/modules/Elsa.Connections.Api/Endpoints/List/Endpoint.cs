using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Models;

namespace Elsa.Connections.Api.Endpoints.List;

public class Endpoint(IConnectionRepository store) : ElsaEndpointWithoutRequest<PagedListResponse<ConnectionConfigurationMetadataModel>>
{
    public override void Configure()
    {
        Get("/connection-configuration");
        AllowAnonymous();
    }

    public override async Task<PagedListResponse<ConnectionConfigurationMetadataModel>> ExecuteAsync(CancellationToken ct)
    {
        var config = await store.GetConnectionsAsync();
        return new PagedListResponse<ConnectionConfigurationMetadataModel>(Page.Of(config, config.Count()));
    }
}
