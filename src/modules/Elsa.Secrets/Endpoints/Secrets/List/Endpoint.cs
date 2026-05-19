using Elsa.Abstractions;
using Elsa.Secrets.Permissions;
using Elsa.Secrets.Services;

namespace Elsa.Secrets.Endpoints.Secrets.List;

internal class Endpoint(ISecretManager manager) : ElsaEndpoint<ListSecretsRequest, ListSecretsResponse>
{
    public override void Configure()
    {
        Get("/secrets");
        ConfigurePermissions(SecretsPermissions.Read);
    }

    public override async Task<ListSecretsResponse> ExecuteAsync(ListSecretsRequest request, CancellationToken cancellationToken)
    {
        var items = await manager.ListAsync(request, cancellationToken);
        var models = items.Select(x => x.ToModel()).ToList();
        return new ListSecretsResponse { Items = models, TotalCount = models.Count };
    }
}
