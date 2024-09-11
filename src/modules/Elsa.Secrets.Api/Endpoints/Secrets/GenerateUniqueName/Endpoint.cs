using Elsa.Abstractions;
using Elsa.Secrets.Management;
using Elsa.Secrets.UniqueName;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.GenerateUniqueName;

/// Generates a unique name for a secret.
[UsedImplicitly]
public class Endpoint(ISecretManager manager) : ElsaEndpointWithoutRequest<GenerateUniqueNameResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/actions/secrets/generate-unique-name");
        ConfigurePermissions("secrets:write");
    }

    /// <inheritdoc />
    public override async Task<GenerateUniqueNameResponse> ExecuteAsync(CancellationToken ct)
    {
        var newName = await manager.GenerateUniqueNameAsync(ct);
        return new(newName);
    }
}