using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Connections.Api.Extensions;
using Elsa.Connections.Models;
using Elsa.Connections.Persistence.Contracts;
using Elsa.Models;

namespace Elsa.Connections.Api.Endpoints.List;

public class Endpoint(IConnectionStore store) : ElsaEndpointWithoutRequest<PagedListResponse<ConnectionModel>>
{
    public override void Configure()
    {
        Get("/connection-configuration");
        ConfigurePermissions($"{Constants.PermissionsNamespace}:read");
    }

    public override async Task<PagedListResponse<ConnectionModel>> ExecuteAsync(CancellationToken ct)
    {
        var entities = await store.ListAsync(ct);
        var models = entities.Select(x => x.ToModel()).ToList();
        return new(Page.Of(models, models.Count()));
    }
}
