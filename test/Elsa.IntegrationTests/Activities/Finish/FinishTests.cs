using System.Linq;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Services;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Activities;

public class FinishTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public FinishTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Subsequent activities are not executed")]
    public async Task Test1()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow<FinishSequentialWorkflow>();
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Line 1" }, lines);
    }
    
    [Fact(DisplayName = "Workflow status is set to Finished")]
    public async Task Test2()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow<FinishSequentialWorkflow>();
        var result = await _workflowRunner.RunAsync(workflow);
        var workflowState = result.WorkflowState;
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, workflowState.SubStatus);
    }
    
    [Fact(DisplayName = "All bookmarks are removed")]
    public async Task Test3()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow<FinishForkedWorkflow>();
        var result = await _workflowRunner.RunAsync(workflow);
        Assert.Empty(result.Bookmarks);
    }
}