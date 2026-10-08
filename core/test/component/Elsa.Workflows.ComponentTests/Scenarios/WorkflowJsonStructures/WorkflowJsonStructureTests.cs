using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowJsonStructures;

public class WorkflowJsonStructureTests(App app) : AppComponentTest(app)
{
    private const string MainDefinitionId = "a417fadbbb7c417e";
    private const string Sub1NodeId = "Workflow2:4d5af7585eece1d7:f19ae76011a020f3";

    [Fact]
    public async Task Workflow_ContainingWorkflowActivity_ShouldNotIncludeChildrenOfWorkflowActivity()
    {
        var client = WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();
        var workflowDefinition = (await client.GetByIdAsync(MainDefinitionId))!;
        var root = workflowDefinition.Root;
        var subActivity = root["activities"]![0]!;

        Assert.Null(subActivity["root"]);
    }

    [Fact]
    public async Task Requesting_Subgraph_Returns_ExpectedSubgraph()
    {
        var client = WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();
        var subgraph = (await client.GetSubgraphAsync(MainDefinitionId, Sub1NodeId))!;
        Assert.Equal(Sub1NodeId, subgraph.Activity["nodeId"]!.ToString());
        var writeLine = subgraph.Activity["root"]!["root"]!["activities"]![0]!;
        Assert.Equal("Elsa.WriteLine", writeLine["type"]!.ToString());
    }
}