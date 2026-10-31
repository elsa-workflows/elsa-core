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

        // The designer needs the activity descriptors to open any definition, so every signed-in user may read them. Besides the installed
        // activities this includes workflows marked as usable as an activity, whose name, description and inputs are therefore visible too.
        RequireAuthenticatedOnly();
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