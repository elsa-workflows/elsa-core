using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowJsonStructures;

public class WorkflowJsonStructureTests(App app) : AppComponentTest(app)
{
    [Fact]
    public async Task Workflow_ContainingWorkflowActivity_ShouldNotIncludeChildrenOfWorkflowActivity()
    {
        var client = WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();
        var workflowDefinition = (await client.GetByIdAsync("a417fadbbb7c417e"))!;
        var root = workflowDefinition.Root;
        var subActivity = root["activities"]![0]!;

        Assert.Null(subActivity["root"]);
    }
}