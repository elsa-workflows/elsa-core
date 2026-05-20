using Elsa.Abstractions;
using Elsa.Secrets.Permissions;

namespace Elsa.Secrets.Endpoints.Secrets.Descriptors;

internal class Endpoint(ISecretTypeRegistry typeRegistry, ISecretStoreRegistry storeRegistry) : ElsaEndpointWithoutRequest<SecretDescriptorsResponse>
{
    public override void Configure()
    {
        Get("/secrets/descriptors");
        ConfigurePermissions(SecretsPermissions.Read);
    }

    public override Task<SecretDescriptorsResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new SecretDescriptorsResponse
        {
            Types = typeRegistry.List().ToList(),
            Stores = storeRegistry.List().Select(x => x.Descriptor).ToList()
        });
    }
}
