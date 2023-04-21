using System.Linq;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Activities;

public class ForkTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;

    public ForkTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowBuilderFactory = services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Each branch executes")]
    public async Task Test1()
    {
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<BasicForkWorkflow>();
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[]{ "Branch 3", "Branch 2", "Branch 1" }, lines);
    }
    
    [Fact(DisplayName = "Wait AnyAsync causes workflow to continue")]
    public async Task Test2()
    {
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<JoinAnyForkWorkflow>();
        
        // First run.
        var result = await _workflowRunner.RunAsync(workflow);
        
        // Collect one of the bookmarks to resume the workflow.
        var bookmark = result.WorkflowState.Bookmarks.FirstOrDefault(x => x.ActivityNodeId == "Workflow1:Sequence1:Fork1:Sequence3:Event2");
        Assert.NotNull(bookmark);
        
        // Resume the workflow.
        var runOptions = new RunWorkflowOptions(bookmarkId: bookmark!.Id);
        await _workflowRunner.RunAsync(workflow, result.WorkflowState, runOptions);
        
        // Verify output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[]{ "Start", "Branch 2", "End" }, lines);
    }
}