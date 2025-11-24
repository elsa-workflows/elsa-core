using Elsa.Testing.Shared;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.SmokeTests;

/// <summary>
/// Smoke tests that verify basic functionality of core workflow activities.
/// </summary>
public class ActivitiesSmokeTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "Smoke test executes all core activities successfully")]
    public async Task SmokeTest_ExecutesAllActivities_Successfully()
    {
        // Act
        var (result, _) = await RunWorkflowAndCaptureOutput();

        // Assert - Workflow completed successfully
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);

        // Verify outputs were set correctly
        var outputs = result.WorkflowState.Output;
        Assert.NotNull(outputs);
        Assert.True(outputs.ContainsKey("FinalResult"));

        var finalResult = outputs["FinalResult"]?.ToString();
        Assert.NotNull(finalResult);
        Assert.Contains("Counter=10", finalResult);
        Assert.Contains("Name=Updated Name", finalResult);
        Assert.Contains("Result=Switch-2", finalResult);
    }

    [Fact(DisplayName = "Smoke test verifies all activities were executed")]
    public async Task SmokeTest_VerifiesActivityExecution()
    {
        // Act
        var (_, lines) = await RunWorkflowAndCaptureOutput();

        // Assert - Verify key activities executed by checking WriteLine outputs
        Assert.Contains(lines, line => line.Contains("=== Smoke Test Started ==="));
        Assert.Contains(lines, line => line.Contains("Name: Updated Name"));
        Assert.Contains(lines, line => line.Contains("Untyped: Untyped value"));
        Assert.Contains(lines, line => line.Contains("If branch: True path executed"));
        Assert.Contains(lines, line => line.Contains("Switch: Case 2 executed"));
        Assert.Contains(lines, line => line.Contains("For loop: Completed with 3 iterations"));
        Assert.Contains(lines, line => line.Contains("While loop: Completed with 3 iterations"));
        Assert.Contains(lines, line => line.Contains("ForEach loop: Completed with 2 items processed"));

        // Verify activities after Complete did NOT execute
        Assert.DoesNotContain(lines, line => line.Contains("After Complete (should not execute)"));
    }

    [Fact(DisplayName = "Break activity works correctly in different loop contexts")]
    public async Task SmokeTest_BreakActivity_WorksInDifferentLoops()
    {
        // Act
        var (_, lines) = await RunWorkflowAndCaptureOutput();

        // Assert - Verify Break worked in For loop (stopped at 3 iterations, not 100)
        Assert.Contains(lines, line => line.Contains("For loop: Completed with 3 iterations"));

        // Verify Break worked in While loop (stopped at 3 iterations, didn't run infinitely)
        Assert.Contains(lines, line => line.Contains("While loop: Iteration 1"));
        Assert.Contains(lines, line => line.Contains("While loop: Iteration 2"));
        Assert.Contains(lines, line => line.Contains("While loop: Iteration 3"));
        Assert.Contains(lines, line => line.Contains("While loop: Completed with 3 iterations"));

        // Verify Break worked in ForEach (stopped after 2 items: A, B, not C)
        Assert.Contains(lines, line => line.Contains("ForEach: Item 'A'"));
        Assert.Contains(lines, line => line.Contains("ForEach: Item 'B'"));
        Assert.DoesNotContain(lines, line => line.Contains("ForEach: Item 'C'"));
        Assert.Contains(lines, line => line.Contains("ForEach loop: Completed with 2 items processed"));
    }

    [Fact(DisplayName = "Switch activity executes correct case")]
    public async Task SmokeTest_SwitchActivity_ExecutesCorrectCase()
    {
        // Act
        var (_, lines) = await RunWorkflowAndCaptureOutput();

        // Assert - Only Case 2 should execute
        Assert.DoesNotContain(lines, line => line.Contains("Switch: Case 1 (should not execute)"));
        Assert.Contains(lines, line => line.Contains("Switch: Case 2 executed"));
        Assert.DoesNotContain(lines, line => line.Contains("Switch: Case 3 (should not execute)"));
        Assert.DoesNotContain(lines, line => line.Contains("Switch: Default (should not execute)"));
    }

    [Fact(DisplayName = "If activity executes correct branch")]
    public async Task SmokeTest_IfActivity_ExecutesCorrectBranch()
    {
        // Act
        var (_, lines) = await RunWorkflowAndCaptureOutput();

        // Assert - Only Then branch should execute
        Assert.Contains(lines, line => line.Contains("If branch: True path executed"));
        Assert.DoesNotContain(lines, line => line.Contains("If branch: False path (should not execute)"));
    }
    
    private async Task<(RunWorkflowResult Result, List<string> Lines)> RunWorkflowAndCaptureOutput()
    {
        var workflow = new ActivitiesSmokeTestWorkflow();
        var result = await _fixture.RunWorkflowAsync(workflow);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        return (result, lines);
    }
}
