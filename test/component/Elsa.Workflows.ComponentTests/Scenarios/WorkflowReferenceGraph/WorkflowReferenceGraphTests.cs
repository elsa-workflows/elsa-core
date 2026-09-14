using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
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
    private const string ChildDefinitionId = "refgraph-child";
    private const string ParentDefinitionId = "refgraph-parent";

    [Test]
    [DisplayName("Consumers endpoint returns direct and transitive consumers")]
    public async Task ConsumersEndpoint_ReturnsTransitiveConsumers()
    {
        var client = WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();

        var response = await client.GetConsumersAsync(GrandchildDefinitionId);

        await Assert.That(response).IsNotNull();
        await Assert.That(response.ConsumingWorkflowDefinitionIds).Contains(ChildDefinitionId);
        await Assert.That(response.ConsumingWorkflowDefinitionIds).Contains(ParentDefinitionId);
    }

    [Test]
    [DisplayName("Consumers endpoint for leaf workflow returns empty list")]
    public async Task ConsumersEndpoint_LeafWorkflow_ReturnsEmpty()
    {
        var client = WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();

        var response = await client.GetConsumersAsync(ParentDefinitionId);

        await Assert.That(response).IsNotNull();
        await Assert.That(response.ConsumingWorkflowDefinitionIds).IsEmpty();
    }

    [Test]
    [DisplayName("Consumers endpoint for unknown workflow returns not found")]
    public async Task ConsumersEndpoint_UnknownWorkflow_ReturnsNotFound()
    {
        var client = WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();
        const string unknownDefinitionId = "refgraph-unknown-definition";

        var exception = (await Assert.ThrowsExactlyAsync<Refit.ApiException>(async () =>
            await client.GetConsumersAsync(unknownDefinitionId)))!;

        await Assert.That(exception.StatusCode).IsEqualTo(System.Net.HttpStatusCode.NotFound);
    }
}
