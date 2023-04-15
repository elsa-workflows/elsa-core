using Elsa.Abstractions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.Get;

[PublicAPI]
internal class Get : ElsaEndpoint<Request, ActivityDescriptor>
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
        var descriptor = request.Version == null ? _registry.Find(request.TypeName) : _registry.Find(request.TypeName, request.Version.Value);

        if (descriptor == null)
            await SendNotFoundAsync(cancellationToken);
        else
            await SendOkAsync(descriptor, cancellationToken);
    }
}