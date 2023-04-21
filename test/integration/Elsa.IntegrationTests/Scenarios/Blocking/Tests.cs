using System.Linq;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.Blocking;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowBuilderFactory = services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Subsequent activity does not get scheduled when previous activity created a bookmark")]
    public async Task Test1()
    {
        await _workflowRunner.RunAsync<BlockingSequentialWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Line 1" }, lines);
    }
    
    [Fact(DisplayName = "Subsequent activities are scheduled when resuming workflow using bookmark")]
    public async Task Test2()
    {
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<BlockingSequentialWorkflow>();
        
        // Start workflow.
        var result = await _workflowRunner.RunAsync(workflow);
        var bookmark = result.WorkflowState.Bookmarks.FirstOrDefault(x => x.ActivityNodeId == "Workflow1:Sequence1:Resume");

        // Resume workflow.
        var runOptions = new RunWorkflowOptions(bookmarkId: bookmark!.Id);
        await _workflowRunner.RunAsync(workflow, result.WorkflowState, runOptions);
        
        // Verify expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Line 1", "Line 2", "Line 3" }, lines);
    }
}