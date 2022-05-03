using System.Linq;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.Blocking;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Subsequent activity does not get scheduled when previous activity created a bookmark")]
    public async Task Test1()
    {
        var workflow = await new WorkflowDefinitionBuilder().BuildWorkflowAsync<BlockingSequentialWorkflow>();
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Line 1" }, lines);
    }
    
    [Fact(DisplayName = "Subsequent activities are scheduled when resuming workflow using bookmark")]
    public async Task Test2()
    {
        var workflow = await new WorkflowDefinitionBuilder().BuildWorkflowAsync<BlockingSequentialWorkflow>();
        
        // Start workflow.
        var result = await _workflowRunner.RunAsync(workflow);
        var bookmark = result.Bookmarks.FirstOrDefault(x => x.ActivityId == "Resume");

        // Resume workflow.
        await _workflowRunner.RunAsync(workflow, result.WorkflowState, bookmark);
        
        // Verify expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Line 1", "Line 2", "Line 3" }, lines);
    }
}