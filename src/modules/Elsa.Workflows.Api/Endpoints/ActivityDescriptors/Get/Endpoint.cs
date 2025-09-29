using Elsa.Abstractions;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptors.Get;

[PublicAPI]
internal class Get : ElsaEndpoint<Request, ActivityDescriptor>
{
    private readonly IActivityRegistryLookupService _registryLookup;

    /// <inheritdoc />
    public Get(IActivityRegistryLookupService registryLookup)
    {
        _registryLookup = registryLookup;
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
        var descriptor = request.Version == null ? await _registryLookup.FindAsync(request.TypeName) : await _registryLookup.FindAsync(request.TypeName, request.Version.Value);

        if (descriptor == null)
            await Send.NotFoundAsync(cancellationToken);
        else
            await Send.OkAsync(descriptor, cancellationToken);
    }
}