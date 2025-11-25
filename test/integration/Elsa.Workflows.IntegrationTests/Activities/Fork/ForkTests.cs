using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Activities.Workflows;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Activities;

/// <summary>
/// Integration tests for the <see cref="Workflows.Activities.Fork"/> activity.
/// Tests Fork behavior with different join modes and branch configurations.
/// </summary>
public class ForkTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "Fork executes all branches with WaitAll")]
    public async Task Fork_ExecutesAllBranchesWithWaitAll()
    {
        // Act
        var result = await _fixture.RunWorkflowAsync(new BasicForkWorkflow());
        var lines = _fixture.CapturingTextWriter.Lines.ToList();

        // Assert
        Assert.Equal(new[] { "Branch 1", "Branch 2", "Branch 3" }, lines);
    }

    [Fact(DisplayName = "Fork with WaitAny continues after first branch completes")]
    public async Task Fork_WaitAnyContinuesAfterFirstBranch()
    {
        // Arrange & build services
        await _fixture.BuildAsync();
        var workflowBuilderFactory = _fixture.Services.GetRequiredService<IWorkflowBuilderFactory>();
        var workflow = await workflowBuilderFactory.CreateBuilder().BuildWorkflowAsync<JoinAnyForkWorkflow>();

        // Act - First run
        var workflowRunner = _fixture.Services.GetRequiredService<IWorkflowRunner>();
        var result = await workflowRunner.RunAsync(workflow);

        // Collect one of the bookmarks to resume the workflow
        var bookmark = result.WorkflowState.Bookmarks.FirstOrDefault(x => x.ActivityId == "Event2");
        Assert.NotNull(bookmark);

        // Resume the workflow
        var runOptions = new RunWorkflowOptions { BookmarkId = bookmark.Id };
        await workflowRunner.RunAsync(workflow, result.WorkflowState, runOptions);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();

        // Assert
        Assert.Equal(new[] { "Start", "Branch 2", "End" }, lines);
    }

    [Fact(DisplayName = "Fork with no branches completes successfully")]
    public async Task Fork_WithNoBranchesCompletesSuccessfully()
    {
        // Act
        var result = await _fixture.RunWorkflowAsync(new EmptyForkWorkflow());
        var lines = _fixture.CapturingTextWriter.Lines.ToList();

        // Assert
        Assert.Equal(new[] { "Before fork", "After fork" }, lines);
    }
}