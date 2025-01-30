using Elsa.Abstractions;
using Elsa.Models;
using Elsa.Workflows.CommitStates;

namespace Elsa.Workflows.Api.Endpoints.CommitStrategies.Workflows.List;

/// <summary>
/// Represents an API endpoint that provides a list of registered workflow commit strategies.
/// </summary>
/// <remarks>
/// This class is an implementation of an endpoint that retrieves a collection of workflow commit strategy registrations
/// from a provided registry and returns them in a unified response.
/// </remarks>
internal class List(ICommitStrategyRegistry registry) : ElsaEndpointWithoutRequest<ListResponse<WorkflowCommitStrategyRegistration>>
{
    public override void Configure()
    {
        Get("/descriptors/commit-strategies/workflows");
        ConfigurePermissions("read:commit-strategies");
    }

    public override Task<ListResponse<WorkflowCommitStrategyRegistration>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var descriptors = registry.ListWorkflowStrategyRegistrations().ToList();
        var response =new ListResponse<WorkflowCommitStrategyRegistration>(descriptors);
        return Task.FromResult(response);
    }
}