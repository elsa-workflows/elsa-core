using Elsa.Abstractions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.Get;

public class Get : ElsaEndpoint<Request, ActivityDescriptor>
{
    private readonly IActivityRegistry _registry;

    /// <inheritdoc />
    public Get(IActivityRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/descriptors/activities/{typeName}");
        ConfigurePermissions("read:*", "read:activity-descriptors");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var descriptor = _registry.Find(request.TypeName);

        if (descriptor == null)
            await SendNotFoundAsync(cancellationToken);
        else
            await SendOkAsync(descriptor, cancellationToken);
    }
}