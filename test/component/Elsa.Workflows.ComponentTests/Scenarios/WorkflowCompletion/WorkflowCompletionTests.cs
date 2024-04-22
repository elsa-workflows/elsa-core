using System.Net;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;
using Xunit.Abstractions;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowCompletion;

public class WorkflowCompletionTests(ITestOutputHelper testOutputHelper, WorkflowServerWebAppFactoryFixture factoryFixture) : ComponentTest(testOutputHelper, factoryFixture)
{
    [Theory]
    [InlineData("hello-world")]
    [InlineData("fork-1")]
    public async Task Workflow_ShouldComplete(string workflowDefinitionId)
    {
        var client = FactoryFixture.CreateApiClient<IExecuteWorkflowApi>();
        using var response = await client.ExecuteAsync(workflowDefinitionId);
        var model = await response.ReadAsJsonAsync<Response>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WorkflowSubStatus.Finished, model.WorkflowState.SubStatus);
    }
}