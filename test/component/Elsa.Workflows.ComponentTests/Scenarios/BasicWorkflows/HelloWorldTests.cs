using System.Net;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Testing.Shared.Extensions;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.BasicWorkflows;

public class HelloWorldTests(App app) : AppComponentTest(app)
{
    [Test]
    public async Task HelloWorldWorkflow_ShouldReturnOk()
    {
        var client = WorkflowServer.CreateApiClient<IExecuteWorkflowApi>();
        using var response = await client.ExecuteAsync("1590068018aa4f0a");
        var model = await response.ReadAsJsonAsync<Response>(WorkflowServer.Services);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(model.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);
    }
}