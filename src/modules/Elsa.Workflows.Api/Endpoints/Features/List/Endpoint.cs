using Elsa.Abstractions;
using Elsa.Features.Contracts;
using Elsa.Features.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.Features.List;

/// <summary>
/// Returns a list of installed features.
/// </summary>
[PublicAPI]
internal class List : ElsaEndpointWithoutRequest<Response>
{
    private readonly IInstalledFeatureRegistry _installedFeatureRegistry;

    /// <inheritdoc />
    public List(IInstalledFeatureRegistry installedFeatureRegistry)
    {
        _installedFeatureRegistry = installedFeatureRegistry;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/features/installed");
        ConfigurePermissions("read:*", "read:installed-features");
    }

    /// <inheritdoc />
    public override Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var descriptors = _installedFeatureRegistry.List().ToList();
        var response = new Response(descriptors);

        return Task.FromResult(response);
    }
}

[PublicAPI]
internal record Response(ICollection<FeatureDescriptor> InstalledFeatures);