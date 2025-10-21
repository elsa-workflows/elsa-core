using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
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
        // Populate registries
        await _services.PopulateRegistriesAsync();

        // Execute parent workflow
        var workflowState = await _services.RunWorkflowUntilEndAsync("parent-no-wait");

        // Assert parent workflow completed
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, workflowState.SubStatus);

        // Assert both parent and child wrote output
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Contains("Parent: Before child", lines);
        Assert.Contains("Child: Executing", lines);
        Assert.Contains("Parent: After child", lines);
    }

    [Fact(DisplayName = "ExecuteWorkflow with WaitForCompletion waits for child to finish")]
    public async Task ExecuteWorkflowWithWaitForCompletion()
    {
        // Populate registries
        await _services.PopulateRegistriesAsync();

        // Execute parent workflow
        var workflowState = await _services.RunWorkflowUntilEndAsync("parent-with-wait");

        // Assert parent workflow completed
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, workflowState.SubStatus);

        // Assert execution order: parent before, child, then parent after
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(3, lines.Count);
        Assert.Equal("Parent: Before child", lines[0]);
        Assert.Equal("Child: Executing", lines[1]);
        Assert.Equal("Parent: After child", lines[2]);
    }

    [Fact(DisplayName = "ExecuteWorkflow passes input to child workflow")]
    public async Task ExecuteWorkflowPassesInput()
    {
        // Populate registries
        await _services.PopulateRegistriesAsync();

        // Execute parent workflow
        var workflowState = await _services.RunWorkflowUntilEndAsync("parent-with-input");

        // Assert workflows completed
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);

        // Assert child received and used the input
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Contains("Child received: Hello from parent", lines);
    }

    [Fact(DisplayName = "ExecuteWorkflow captures child workflow output")]
    public async Task ExecuteWorkflowCapturesOutput()
    {
        // Populate registries
        await _services.PopulateRegistriesAsync();

        // Execute parent workflow
        var workflowState = await _services.RunWorkflowUntilEndAsync("parent-capture-output");

        // Assert workflows completed
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);

        // Assert parent received and used child's output
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Contains("Child output: 42", lines);
        Assert.Contains("Parent received: 42", lines);
    }

    [Fact(DisplayName = "ExecuteWorkflow sets correlation ID on child workflow")]
    public async Task ExecuteWorkflowSetsCorrelationId()
    {
        // Populate registries
        await _services.PopulateRegistriesAsync();

        // Execute parent workflow
        var workflowState = await _services.RunWorkflowUntilEndAsync("parent-with-correlation");

        // Assert workflows completed
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);

        // Get workflow instance store to verify correlation ID was set
        var workflowInstanceStore = _services.GetRequiredService<IWorkflowInstanceStore>();
        var childInstance = await workflowInstanceStore.FindAsync(new()
        {
            DefinitionId = "child-workflow"
        });
        
        Assert.Equal("test-correlation-123", childInstance.CorrelationId);
    }

    [Fact(DisplayName = "ExecuteWorkflow includes ParentInstanceId in child properties")]
    public async Task ExecuteWorkflowSetsParentInstanceId()
    {
        // Populate registries
        await _services.PopulateRegistriesAsync();

        // Execute parent workflow
        var workflowState = await _services.RunWorkflowUntilEndAsync("parent-no-wait");

        // Get workflow instance store
        var workflowInstanceStore = _services.GetRequiredService<IWorkflowInstanceStore>();

        // Get parent instance
        var parentInstance = await workflowInstanceStore.FindAsync(new()
        {
            DefinitionId = "parent-no-wait"
        });

        // Get child instance
        var childInstance = await workflowInstanceStore.FindAsync(new()
        {
            DefinitionId = "child-workflow"
        });

        // Assert child has ParentInstanceId property set
        Assert.True(childInstance.WorkflowState.Properties.ContainsKey("ParentInstanceId"));
        Assert.Equal(parentInstance.Id, childInstance.WorkflowState.Properties["ParentInstanceId"]);
    }
}
