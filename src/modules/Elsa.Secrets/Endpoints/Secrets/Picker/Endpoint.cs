using Elsa.Abstractions;
using Elsa.Secrets.Permissions;
using Elsa.Secrets.Services;

namespace Elsa.Secrets.Endpoints.Secrets.Picker;

internal class Endpoint(ISecretManager manager) : ElsaEndpoint<SecretPickerRequest, SecretPickerResponse>
{
    public override void Configure()
    {
        Post("/secrets/picker");
        ConfigurePermissions(SecretsPermissions.Read);
    }

    public override async Task<SecretPickerResponse> ExecuteAsync(SecretPickerRequest request, CancellationToken cancellationToken)
    {
        var listRequest = new ListSecretsRequest
        {
            Search = request.Search,
            TypeNames = request.TypeNames,
            StoreNames = request.StoreNames,
            Scope = request.Scope,
            Status = request.ActiveOnly ? SecretStatus.Active : null,
            PageSize = 100
        };

        var items = await manager.ListAsync(listRequest, cancellationToken);
        var models = items
            .Select(x => x.ToModel())
            .ToList();

        return new SecretPickerResponse { Items = models, CanCreateInline = true };
    }
}
