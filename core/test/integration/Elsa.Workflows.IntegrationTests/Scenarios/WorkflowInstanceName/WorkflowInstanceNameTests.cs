using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Scenarios.WorkflowInstanceName.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowInstanceName;

public class WorkflowInstanceNameTests
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowRuntime _workflowRuntime;

    public WorkflowInstanceNameTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddWorkflow<NamedWorkflow>()
            .Build();

        _workflowRuntime = _services.GetRequiredService<IWorkflowRuntime>();
    }

    [Fact(DisplayName = "Setting a workflow instance name keeps the workflow instance name when the workflow is executed")]
    public async Task SuspendedCancelTest()
    {
        await _services.PopulateRegistriesAsync();
        const string workflowDefinitionId = nameof(NamedWorkflow);
        var desiredName = Guid.NewGuid().ToString();
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new()
        {
            Name = desiredName,
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(workflowDefinitionId, VersionOptions.Published)
        });
        await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);
        var workflowState = await workflowClient.ExportStateAsync();

        Assert.Equal([desiredName], _capturingTextWriter.Lines);
        Assert.Equal(desiredName, workflowState.Name);
    }
}