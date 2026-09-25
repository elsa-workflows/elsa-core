using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Permissions;
using Elsa.Secrets.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Endpoints.Secrets.Picker;

internal class Endpoint(ISecretManager manager, ISecretStoreRegistry storeRegistry) : ElsaEndpoint<SecretPickerRequest, SecretPickerResponse>
{
    public override void Configure()
    {
        Post("/secrets/picker");
        RequirePermission(Elsa.Secrets.Permissions.SecretsResourcePermissions.Secrets, CoreVerbs.View);
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
        var permissionEvaluator = HttpContext.RequestServices.GetService<IPermissionEvaluator>() ?? PermissionEvaluator.Shared;
        var hasWritePermission = !EndpointSecurityOptions.SecurityIsEnabled ||
                                 permissionEvaluator.HasPermission(HttpContext.User, SecretsResourcePermissions.Secrets, CoreVerbs.Write);
        var canCreate = hasWritePermission && storeRegistry.List().Any(x => !x.Descriptor.IsReadOnly);

        return new SecretPickerResponse { Items = models, CanCreateInline = canCreate };
    }
}
