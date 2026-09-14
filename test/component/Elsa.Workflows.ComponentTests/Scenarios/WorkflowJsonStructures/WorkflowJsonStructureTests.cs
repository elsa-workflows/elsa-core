using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowJsonStructures;

public class WorkflowJsonStructureTests(App app) : AppComponentTest(app)
{
    private const string MainDefinitionId = "a417fadbbb7c417e";
    private const string Sub1NodeId = "Workflow2:4d5af7585eece1d7:f19ae76011a020f3";

    [Test]
    public async Task Workflow_ContainingWorkflowActivity_ShouldNotIncludeChildrenOfWorkflowActivity()
    {
        var client = WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();
        var workflowDefinition = (await client.GetByIdAsync(MainDefinitionId))!;
        var root = workflowDefinition.Root;
        var subActivity = root["activities"]![0]!;

        await Assert.That(subActivity["root"]).IsNull();
    }

    [Test]
    public async Task Requesting_Subgraph_Returns_ExpectedSubgraph()
    {
        var client = WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();
        var subgraph = (await client.GetSubgraphAsync(MainDefinitionId, Sub1NodeId))!;
        await Assert.That(subgraph.Activity["nodeId"]!.ToString()).IsEqualTo(Sub1NodeId);
        var writeLine = subgraph.Activity["root"]!["root"]!["activities"]![0]!;
        await Assert.That(writeLine["type"]!.ToString()).IsEqualTo("Elsa.WriteLine");
    }
}