using Elsa.Abstractions;
using Elsa.Secrets.Permissions;

namespace Elsa.Secrets.Endpoints.Secrets.Test;

internal class Endpoint(ISecretManager manager) : ElsaEndpointWithoutRequest<SecretTestResponse>
{
    public override void Configure()
    {
        Post("/secrets/{name}/test");
        ConfigurePermissions(SecretsPermissions.Test);
    }

    public override Task<SecretTestResponse> ExecuteAsync(CancellationToken cancellationToken)
    {
        return manager.TestAsync(Route<string>("name")!, cancellationToken);
    }
}
