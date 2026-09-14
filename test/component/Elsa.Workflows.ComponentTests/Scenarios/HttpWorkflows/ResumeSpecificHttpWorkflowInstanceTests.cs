using System.Net;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.HttpWorkflows;

public class ResumeSpecificHttpWorkflowInstanceTests(App app) : AppComponentTest(app)
{
    [Test]
    [Arguments("workflowInstanceId")]
    [Arguments("correlationId")]
    public async Task ResumingSpecificWorkflow_ShouldResumeSpecifiedWorkflow(string identifierKey)
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();

        // Start 3 instances.
        var workflowInstanceId1 = await StartWorkflowAsync(client, identifierKey);
        var workflowInstanceId2 = await StartWorkflowAsync(client, identifierKey);
        var workflowInstanceId3 = await StartWorkflowAsync(client, identifierKey);

        // Resume the 2nd instance.
        using var response = await ResumeWorkflowAsync(client, identifierKey, workflowInstanceId2);
        
        // Response should be OK.
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    private async Task<string> StartWorkflowAsync(HttpClient client, string identifierKey)
    {
        var identityGenerator = Scope.ServiceProvider.GetRequiredService<IIdentityGenerator>();
        var identifierValue = identityGenerator.GenerateId();
        await client.GetStringAsync($"simple-http-api/start?{identifierKey}={identifierValue}");
        return identifierValue;
    }

    private async Task<HttpResponseMessage> ResumeWorkflowAsync(HttpClient client, string identifierKey, string identifierValue)
    {
        return await client.GetAsync($"simple-http-api/resume?{identifierKey}={identifierValue}");
    }
}
