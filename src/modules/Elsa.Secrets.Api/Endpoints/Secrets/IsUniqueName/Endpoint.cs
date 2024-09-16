using Elsa.Abstractions;
using Elsa.Secrets.Management;
using Elsa.Secrets.UniqueName;
using JetBrains.Annotations;

namespace Elsa.Secrets.Api.Endpoints.Secrets.IsUniqueName;

/// Checks if a name is unique.
[UsedImplicitly]
public class Endpoint(ISecretNameValidator nameValidator) : ElsaEndpoint<IsUniqueNameRequest, IsUniqueNameResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/queries/secrets/is-unique-name");
        ConfigurePermissions("secrets:write");
    }

    /// <inheritdoc />
    public override async Task<IsUniqueNameResponse> ExecuteAsync(IsUniqueNameRequest req, CancellationToken ct)
    {
        var isUnique = await nameValidator.IsNameUniqueAsync(req.Name, req.Id, ct);
        return new(isUnique);
    }
}