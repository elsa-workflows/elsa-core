using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Activities.ExecuteWorkflow;

public class ExecuteWorkflowTests
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public ExecuteWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .WithWorkflowsFromDirectory("Activities", "ExecuteWorkflow", "Workflows")
            .Build();
    }

    [Fact(DisplayName = "ExecuteWorkflow without WaitForCompletion completes immediately")]
    public async Task ExecuteWorkflowWithoutWaitForCompletion()
    {
        var workflowState = await RunWorkflowAsync("parent-no-wait");

        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, workflowState.SubStatus);

        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Contains("Parent: Before child", lines);
        Assert.Contains("Child: Executing", lines);
        Assert.Contains("Parent: After child", lines);
    }

    [Fact(DisplayName = "ExecuteWorkflow with WaitForCompletion waits for child to finish")]
    public async Task ExecuteWorkflowWithWaitForCompletion()
    {
        var workflowState = await RunWorkflowAsync("parent-with-wait");

        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, workflowState.SubStatus);

        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(3, lines.Count);
        Assert.Equal("Parent: Before child", lines[0]);
        Assert.Equal("Child: Executing", lines[1]);
        Assert.Equal("Parent: After child", lines[2]);
    }

    [Fact(DisplayName = "ExecuteWorkflow passes input to child workflow")]
    public async Task ExecuteWorkflowPassesInput()
    {
        var workflowState = await RunWorkflowAsync("parent-with-input");

        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);

        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Contains("Child received: Hello from parent", lines);
    }

    [Fact(DisplayName = "ExecuteWorkflow captures child workflow output")]
    public async Task ExecuteWorkflowCapturesOutput()
    {
        var workflowState = await RunWorkflowAsync("parent-capture-output");

        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);

        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Contains("Child output: 42", lines);
        Assert.Contains("Parent received: 42", lines);
    }

    [Fact(DisplayName = "ExecuteWorkflow sets correlation ID on child workflow")]
    public async Task ExecuteWorkflowSetsCorrelationId()
    {
        var workflowState = await RunWorkflowAsync("parent-with-correlation");

        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);

        var childInstance = await GetWorkflowInstanceAsync("child-workflow");
        Assert.Equal("test-correlation-123", childInstance.CorrelationId);
    }

    [Fact(DisplayName = "ExecuteWorkflow includes ParentInstanceId in child properties")]
    public async Task ExecuteWorkflowSetsParentInstanceId()
    {
        await RunWorkflowAsync("parent-no-wait");

        var parentInstance = await GetWorkflowInstanceAsync("parent-no-wait");
        var childInstance = await GetWorkflowInstanceAsync("child-workflow");

        Assert.True(childInstance.WorkflowState.Properties.ContainsKey("ParentInstanceId"));
        Assert.Equal(parentInstance.Id, childInstance.WorkflowState.Properties["ParentInstanceId"]);
    }

    private async Task<WorkflowState> RunWorkflowAsync(string workflowDefinitionId)
    {
        await _services.PopulateRegistriesAsync();
        return await _services.RunWorkflowUntilEndAsync(workflowDefinitionId);
    }

    private async Task<WorkflowInstance> GetWorkflowInstanceAsync(string definitionId)
    {
        var workflowInstanceStore = _services.GetRequiredService<IWorkflowInstanceStore>();
        return (await workflowInstanceStore.FindAsync(new()
        {
            DefinitionId = definitionId
        }))!;
    }
}