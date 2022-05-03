using System.Linq;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Activities;

public class ForkTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public ForkTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Each branch executes")]
    public async Task Test1()
    {
        var workflow = await new WorkflowDefinitionBuilder().BuildWorkflowAsync<BasicForkWorkflow>();
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[]{ "Branch 1", "Branch 2", "Branch 3" }, lines);
    }
    
    [Fact(DisplayName = "Wait Any causes workflow to continue")]
    public async Task Test2()
    {
        var workflow = await new WorkflowDefinitionBuilder().BuildWorkflowAsync<JoinAnyForkWorkflow>();
        
        // First run.
        var result = await _workflowRunner.RunAsync(workflow);
        
        // Collect one of the bookmarks to resume the workflow.
        var bookmark = result.Bookmarks.FirstOrDefault(x => x.ActivityId == "Event2");
        Assert.NotNull(bookmark);
        
        // Resume the workflow.
        await _workflowRunner.RunAsync(workflow, result.WorkflowState, bookmark);
        
        // Verify output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[]{ "Start", "Branch 2", "End" }, lines);
    }
}