using Elsa.Testing.Shared;
using Elsa.Workflows.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Activities;

public class FinishTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public FinishTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Subsequent activities are not executed")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<FinishSequentialWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Line 1" }, lines);
    }
    
    [Fact(DisplayName = "Workflow status is set to Finished")]
    public async Task Test2()
    {
        await _services.PopulateRegistriesAsync();
        var result = await _workflowRunner.RunAsync<FinishSequentialWorkflow>();
        var workflowState = result.WorkflowState;
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, workflowState.SubStatus);
    }
    
    [Fact(DisplayName = "All bookmarks are removed")]
    public async Task Test3()
    {
        await _services.PopulateRegistriesAsync();
        var result = await _workflowRunner.RunAsync<FinishSequentialWorkflow>();
        Assert.Empty(result.WorkflowState.Bookmarks);
    }
}