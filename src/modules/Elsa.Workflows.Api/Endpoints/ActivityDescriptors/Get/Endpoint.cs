using Elsa.Abstractions;
using Elsa.Workflows.Contracts;
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
        var descriptor = request.Version == null ? await _registryLookup.Find(request.TypeName) : await _registryLookup.Find(request.TypeName, request.Version.Value);

        if (descriptor == null)
            await SendNotFoundAsync(cancellationToken);
        else
            await SendOkAsync(descriptor, cancellationToken);
    }
}