using System.Linq;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.IntegrationTests.Scenarios.BlockingAndBreaking.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.BlockingAndBreaking;

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

    [Fact(DisplayName = "Fork completes after all branches complete.")]
    public async Task Test1()
    {
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<WaitAllForkWorkflow>();

        // Start workflow.
        var result1 = await _workflowRunner.RunAsync(workflow);

        // Resume the first branch.
        var bookmark = result1.WorkflowState.Bookmarks.FirstOrDefault(x => x.ActivityId == "Branch 1");
        var runOptions = new RunWorkflowOptions(bookmarkId: bookmark!.Id);
        var result2 = await _workflowRunner.RunAsync(workflow, result1.WorkflowState, runOptions);

        // Resume the second branch.
        bookmark = result2.WorkflowState.Bookmarks.FirstOrDefault(x => x.ActivityId == "Branch 2");
        runOptions = new RunWorkflowOptions(bookmarkId: bookmark!.Id);
        var result3 = await _workflowRunner.RunAsync(workflow, result2.WorkflowState, runOptions);

        // Verify expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "Branch 1", "Branch 2", "Branch 1 - Resumed", "Branch 2 - Resumed", "End" }, lines);
    }

    [Fact(DisplayName = "Fork completes after any branch completes.")]
    public async Task Test2()
    {
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<WaitAnyForkWorkflow>();

        // Start workflow.
        var result1 = await _workflowRunner.RunAsync(workflow);

        // Resume the first branch.
        var bookmark = result1.WorkflowState.Bookmarks.FirstOrDefault(x => x.ActivityId == "Branch 1");
        var runOptions = new RunWorkflowOptions(bookmarkId: bookmark!.Id);
        var result2 = await _workflowRunner.RunAsync(workflow, result1.WorkflowState, runOptions);

        // Verify expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "Branch 1", "Branch 2", "Branch 1 - Resumed", "End" }, lines);
    }
    
    [Fact(DisplayName = "Break breaks out of While loop.")]
    public async Task Test3()
    {
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<BreakWhileFromForkWorkflow>();

        // Start workflow.
        var result1 = await _workflowRunner.RunAsync(workflow);

        // Resume the first branch.
        var bookmark = result1.WorkflowState.Bookmarks.FirstOrDefault(x => x.ActivityId == "Branch 1");
        var runOptions = new RunWorkflowOptions(bookmarkId: bookmark!.Id);
        var result2 = await _workflowRunner.RunAsync(workflow, result1.WorkflowState, runOptions);

        // There should be no bookmarks left, since the while loop was broken out of.
        Assert.Empty(result2.WorkflowState.Bookmarks);

        // Verify expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "Branch 1", "Branch 2", "Branch 1 - Resumed", "End" }, lines);
    }
}