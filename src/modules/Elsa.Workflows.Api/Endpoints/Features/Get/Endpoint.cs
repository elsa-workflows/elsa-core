using Elsa.Abstractions;
using Elsa.Features.Contracts;
using Elsa.Features.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.Features.Get;

/// <summary>
/// Returns the specified installed feature.
/// </summary>
[PublicAPI]
internal class Get : ElsaEndpointWithoutRequest<FeatureDescriptor>
{
    private readonly IInstalledFeatureRegistry _installedFeatureRegistry;

    /// <inheritdoc />
    public Get(IInstalledFeatureRegistry installedFeatureRegistry)
    {
        _installedFeatureRegistry = installedFeatureRegistry;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/features/installed/{fullName}");
        ConfigurePermissions("read:*", "read:installed-features");
    }

    /// <inheritdoc />
    public override async Task HandleAsync( CancellationToken cancellationToken)
    {
        var fullName = Route<string>("fullName")!;
        var descriptor = _installedFeatureRegistry.Find(fullName);

        if (descriptor == null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }
        
        await SendOkAsync(descriptor, cancellationToken);
    }
}