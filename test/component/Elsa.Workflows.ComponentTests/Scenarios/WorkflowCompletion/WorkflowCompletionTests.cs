using System.Net;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Testing.Shared.Extensions;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;
using Elsa.Workflows.ComponentTests.Helpers;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowCompletion;

public class WorkflowCompletionTests(App app) : AppComponentTest(app)
{
    [Theory]
    [InlineData("2630068018ac1f0a")]
    [InlineData("5590069018aa4f0e")]
    public async Task Workflow_ShouldComplete(string workflowDefinitionId)
    {
        var client = WorkflowServer.CreateApiClient<IExecuteWorkflowApi>();
        using var response = await client.ExecuteAsync(workflowDefinitionId);
        var model = await response.ReadAsJsonAsync<Response>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WorkflowSubStatus.Finished, model.WorkflowState.SubStatus);
    }
}