using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Activities.ExecuteWorkflow;

public class ExecuteWorkflowTests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public ExecuteWorkflowTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .WithWorkflowsFromDirectory("Activities", "ExecuteWorkflow", "Workflows")
            .Build();
    }

    [Test]
    [DisplayName("ExecuteWorkflow without WaitForCompletion completes immediately")]
    public async Task ExecuteWorkflowWithoutWaitForCompletion()
    {
        var workflowState = await RunWorkflowAsync("parent-no-wait");

        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(workflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).Contains("Parent: Before child");
        await Assert.That(lines).Contains("Child: Executing");
        await Assert.That(lines).Contains("Parent: After child");
    }

    [Test]
    [DisplayName("ExecuteWorkflow with WaitForCompletion waits for child to finish")]
    public async Task ExecuteWorkflowWithWaitForCompletion()
    {
        var workflowState = await RunWorkflowAsync("parent-with-wait");

        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(workflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines.Count).IsEqualTo(3);
        await Assert.That(lines[0]).IsEqualTo("Parent: Before child");
        await Assert.That(lines[1]).IsEqualTo("Child: Executing");
        await Assert.That(lines[2]).IsEqualTo("Parent: After child");
    }

    [Test]
    [DisplayName("ExecuteWorkflow passes input to child workflow")]
    public async Task ExecuteWorkflowPassesInput()
    {
        var workflowState = await RunWorkflowAsync("parent-with-input");

        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);

        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).Contains("Child received: Hello from parent");
    }

    [Test]
    [DisplayName("ExecuteWorkflow captures child workflow output")]
    public async Task ExecuteWorkflowCapturesOutput()
    {
        var workflowState = await RunWorkflowAsync("parent-capture-output");

        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);

        var lines = _capturingTextWriter.Lines.ToList();
        await Assert.That(lines).Contains("Child output: 42");
        await Assert.That(lines).Contains("Parent received: 42");
    }

    [Test]
    [DisplayName("ExecuteWorkflow sets correlation ID on child workflow")]
    public async Task ExecuteWorkflowSetsCorrelationId()
    {
        var workflowState = await RunWorkflowAsync("parent-with-correlation");

        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);

        var childInstance = await GetWorkflowInstanceAsync("child-workflow");
        await Assert.That(childInstance.CorrelationId).IsEqualTo("test-correlation-123");
    }

    [Test]
    [DisplayName("ExecuteWorkflow includes ParentInstanceId in child properties")]
    public async Task ExecuteWorkflowSetsParentInstanceId()
    {
        await RunWorkflowAsync("parent-no-wait");

        var parentInstance = await GetWorkflowInstanceAsync("parent-no-wait");
        var childInstance = await GetWorkflowInstanceAsync("child-workflow");

        await Assert.That(childInstance.WorkflowState.Properties.ContainsKey("ParentInstanceId")).IsTrue();
        await Assert.That(childInstance.WorkflowState.Properties["ParentInstanceId"]).IsEqualTo(parentInstance.Id);
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

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_services);
}
