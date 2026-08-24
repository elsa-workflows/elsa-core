using Elsa.Abstractions;
using Elsa.Authorization;
using Elsa.Identity.Permissions;
using Elsa.Permissions;
using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Permissions.Reach;

/// <summary>What a wildcard grant covers right now.</summary>
/// <param name="Covers">
/// A point-in-time snapshot. A wildcard grant also covers resources registered later, so a role editor
/// should present this as "currently covers", not as a fixed list.
/// </param>
public record Response(string Resource, IReadOnlyCollection<string> Covers, int Count);

/// <summary>The resource pattern to report on, for example <c>workflows/*</c>.</summary>
public class Request
{
    /// <summary>The resource pattern.</summary>
    public string Resource { get; set; } = null!;
}

/// <summary>
/// Reports the resources a grant currently covers. This is the mitigation for forward reach on the
/// resource axis: a wildcard is convenient precisely because it covers things that do not exist yet,
/// so an author needs a way to see what it reaches today.
/// </summary>
[PublicAPI]
internal class Reach(IPermissionDescriptorRegistry registry) : ElsaEndpoint<Request, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/identity/permissions/reach");
        RequirePermission(IdentityPermissions.Roles, CoreVerbs.View);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Resource))
        {
            AddError(nameof(request.Resource), "A resource pattern is required.");
            await Send.ErrorsAsync(cancellation: cancellationToken);
            return;
        }

        var covers = registry.Reach(request.Resource.Trim());

        await Send.OkAsync(new Response(request.Resource.Trim(), covers, covers.Count), cancellationToken);
    }
}
