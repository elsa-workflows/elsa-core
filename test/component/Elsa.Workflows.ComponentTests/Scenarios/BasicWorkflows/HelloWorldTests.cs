using System.Net;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Testing.Framework.Assertions;
using Elsa.Testing.Framework.Models;
using Elsa.Testing.Framework.Services;
using Elsa.Testing.Shared.Extensions;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.BasicWorkflows;

public class HelloWorldTests(App app) : AppComponentTest(app)
{
    [Fact]
    public async Task HelloWorldWorkflow_ShouldReturnOk()
    {
        var client = WorkflowServer.CreateApiClient<IExecuteWorkflowApi>();
        using var response = await client.ExecuteAsync("1590068018aa4f0a");
        var model = await response.ReadAsJsonAsync<Response>(WorkflowServer.Services);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WorkflowSubStatus.Finished, model.WorkflowState.SubStatus);
    }
    
    [Fact]
    public async Task HelloWorldWorkflow_ShouldFinish()
    {
        var scenario = new Scenario
        {
            Id = "1",
            Name = "HelloWorldWorkflow_ShouldFinish",
            Description = "The workflow should finish.",
            Assertions =
            [
                new WorkflowStatusAssertion(WorkflowStatus.Finished),
                new ActivityExecutedAssertion("b039045bb7443e57")
            ]
        };

        var scenarioRunner = Scope.ServiceProvider.GetRequiredService<WorkflowTestScenarioRunner>();
        var results = await scenarioRunner.RunAsync(scenario);

        foreach (var assertionResult in results.AssertionResults) 
            Assert.True(assertionResult.Passed, assertionResult.ErrorMessage);
    }
}