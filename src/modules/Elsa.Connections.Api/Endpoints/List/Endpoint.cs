using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Connections.Api.Extensions;
using Elsa.Connections.Models;
using Elsa.Connections.Persistence.Contracts;
using Elsa.Connections.Persistence.Entities;
using Elsa.Models;

namespace Elsa.Connections.Api.Endpoints.List;

public class Endpoint(IConnectionStore store) : ElsaEndpointWithoutRequest<PagedListResponse<ConnectionModel>>
{
    public override void Configure()
    {
        Get("/connection-configuration");
        AllowAnonymous();
    }

    public override async Task<PagedListResponse<ConnectionModel>> ExecuteAsync(CancellationToken ct)
    {
        var entities = await store.ListAsync();
        var models = entities.Select(x => x.ToModel()).ToList();
        return new PagedListResponse<ConnectionModel>(Page.Of(models, models.Count()));
    }
}
