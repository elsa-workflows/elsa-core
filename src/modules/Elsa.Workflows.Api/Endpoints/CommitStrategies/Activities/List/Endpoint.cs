using Elsa.Abstractions;
using Elsa.Models;
using Elsa.Workflows.CommitStates;

namespace Elsa.Workflows.Api.Endpoints.CommitStrategies.Activities.List;

/// <summary>
/// Represents an API endpoint that provides a list of registered workflow commit strategies.
/// </summary>
/// <remarks>
/// This class is an implementation of an endpoint that retrieves a collection of workflow commit strategy registrations
/// from a provided registry and returns them in a unified response.
/// </remarks>
internal class List(ICommitStrategyRegistry registry) : ElsaEndpointWithoutRequest<ListResponse<CommitStrategyDescriptor>>
{
    public override void Configure()
    {
        Get("/descriptors/commit-strategies/activities");
        ConfigurePermissions("read:commit-strategies");
    }

    public override Task<ListResponse<CommitStrategyDescriptor>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var registrations = registry.ListActivityStrategyRegistrations().ToList();
        var descriptors = CommitStrategyDescriptor.FromStrategyMetadata(registrations.Select(x => x.Metadata)).ToList();
        var response =new ListResponse<CommitStrategyDescriptor>(descriptors);
        return Task.FromResult(response);
    }
}