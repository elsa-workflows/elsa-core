using Elsa.Abstractions;
using Elsa.Features.Contracts;
using Elsa.Features.Models;
using Elsa.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.Features.List;

/// <summary>
/// Returns a list of installed features.
/// </summary>
[PublicAPI]
internal class List(IInstalledFeatureProvider installedFeatureProvider) : ElsaEndpointWithoutRequest<ListResponse<FeatureDescriptor>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/features/installed");

        // Clients such as Studio decide which modules to render from this list, so every signed-in user needs it.
        RequireAuthenticatedOnly();
    }

    /// <inheritdoc />
    public override Task<ListResponse<FeatureDescriptor>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var descriptors = installedFeatureProvider.List().ToList();
        var response = new ListResponse<FeatureDescriptor>(descriptors);

        return Task.FromResult(response);
    }
}