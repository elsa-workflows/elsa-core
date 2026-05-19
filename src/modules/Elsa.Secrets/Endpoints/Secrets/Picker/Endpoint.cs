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
            Scope = request.Scope,
            Status = request.ActiveOnly ? SecretStatus.Active : null,
            PageSize = 100
        };

        var items = await manager.ListAsync(listRequest, cancellationToken);
        var filtered = items
            .Where(x => request.TypeNames.Count == 0 || request.TypeNames.Contains(x.TypeName, StringComparer.OrdinalIgnoreCase))
            .Where(x => request.StoreNames.Count == 0 || request.StoreNames.Contains(x.StoreName, StringComparer.OrdinalIgnoreCase))
            .Select(x => x.ToModel())
            .ToList();

        return new SecretPickerResponse { Items = filtered, CanCreateInline = true };
    }
}
