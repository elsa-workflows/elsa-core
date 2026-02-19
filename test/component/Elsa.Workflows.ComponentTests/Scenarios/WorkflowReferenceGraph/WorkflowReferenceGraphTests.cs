using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowReferenceGraph;

/// <summary>
/// Tests for the Consumers endpoint and Export endpoint with consumer inclusion.
/// Workflow hierarchy: Parent → Child → Grandchild.
/// </summary>
public class WorkflowReferenceGraphTests(App app) : AppComponentTest(app)
{
    private const string GrandchildDefinitionId = "refgraph-grandchild";
    private const string GrandchildVersionId = "refgraph-grandchild-v1";
    private const string ChildDefinitionId = "refgraph-child";
    private const string ChildVersionId = "refgraph-child-v1";
    private const string ParentDefinitionId = "refgraph-parent";

    // --- Consumers endpoint ---

    [Fact(DisplayName = "Consumers endpoint returns direct and transitive consumers")]
    public async Task ConsumersEndpoint_ReturnsTransitiveConsumers()
    {
        var client = WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();

        var response = await client.GetConsumersAsync(GrandchildDefinitionId);

        Assert.NotNull(response);
        Assert.Contains(ChildDefinitionId, response.ConsumingWorkflowDefinitionIds);
        Assert.Contains(ParentDefinitionId, response.ConsumingWorkflowDefinitionIds);
    }

    [Fact(DisplayName = "Consumers endpoint for leaf workflow returns empty list")]
    public async Task ConsumersEndpoint_LeafWorkflow_ReturnsEmpty()
    {
        var client = WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();

        var response = await client.GetConsumersAsync(ParentDefinitionId);

        Assert.NotNull(response);
        Assert.Empty(response.ConsumingWorkflowDefinitionIds);
    }
}
