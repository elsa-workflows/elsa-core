using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.IntegrationTests.Scenarios.BlockingAndBreaking.Workflows;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BlockingAndBreaking;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowBuilderFactory = _services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Fork completes after all branches complete.")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<WaitAllForkWorkflow>();

        // Start workflow.
        var result1 = await _workflowRunner.RunAsync(workflow);

        // Resume the first branch.
        var bookmark = result1.WorkflowState.Bookmarks.FirstOrDefault(x => x.ActivityId == "Branch 1");
        var runOptions = new RunWorkflowOptions { BookmarkId = bookmark!.Id };
        var result2 = await _workflowRunner.RunAsync(workflow, result1.WorkflowState, runOptions);

        // Resume the second branch.
        bookmark = result2.WorkflowState.Bookmarks.FirstOrDefault(x => x.ActivityId == "Branch 2");
        runOptions = new RunWorkflowOptions { BookmarkId = bookmark!.Id };
        var result3 = await _workflowRunner.RunAsync(workflow, result2.WorkflowState, runOptions);

        // Verify expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "Branch 1", "Branch 2", "Branch 1 - Resumed", "Branch 2 - Resumed", "End" }, lines);
    }

    [Fact(DisplayName = "Fork completes after any branch completes.")]
    public async Task Test2()
    {
        await _services.PopulateRegistriesAsync();
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<WaitAnyForkWorkflow>();

        // Start workflow.
        var result1 = await _workflowRunner.RunAsync(workflow);

        // Resume the first branch.
        var bookmark = result1.WorkflowState.Bookmarks.FirstOrDefault(x => x.ActivityId == "Branch 1");
        var runOptions = new RunWorkflowOptions { BookmarkId = bookmark!.Id };
        var result2 = await _workflowRunner.RunAsync(workflow, result1.WorkflowState, runOptions);

        // Verify expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "Branch 1", "Branch 2", "Branch 1 - Resumed", "End" }, lines);
    }

    [Fact(DisplayName = "Break breaks out of While loop.")]
    public async Task Test3()
    {
        await _services.PopulateRegistriesAsync();
        var workflow = await _workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<BreakWhileFromForkWorkflow>();

        // Start workflow.
        var result1 = await _workflowRunner.RunAsync(workflow);

        // Resume the first branch.
        var bookmark = result1.WorkflowState.Bookmarks.FirstOrDefault(x => x.ActivityId == "Branch 1");
        var runOptions = new RunWorkflowOptions { BookmarkId = bookmark!.Id };
        var result2 = await _workflowRunner.RunAsync(workflow, result1.WorkflowState, runOptions);

        // There should be no bookmarks left, since the while loop was broken out of.
        Assert.Empty(result2.WorkflowState.Bookmarks);

        // Verify expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "Branch 1", "Branch 2", "Branch 1 - Resumed", "End" }, lines);
    }
}