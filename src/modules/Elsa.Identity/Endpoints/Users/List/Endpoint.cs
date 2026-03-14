using Elsa.Abstractions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Users.List;

/// <summary>
/// An endpoint that lists all users. Requires the <code>SecurityRoot</code> policy.
/// </summary>
[PublicAPI]
internal class List(IUserStore userStore) : ElsaEndpointWithoutRequest<Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/identity/users");
        ConfigurePermissions("read:user");
        Policies(IdentityPolicyNames.SecurityRoot);
    }

    /// <inheritdoc />
    public override async Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var users = await userStore.FindManyAsync(new UserFilter(), cancellationToken);

        var response = new Response(users
            .Select(user => new UserSummary(user.Id, user.Name, user.Roles, user.TenantId))
            .ToList());

        return response;
    }
}

