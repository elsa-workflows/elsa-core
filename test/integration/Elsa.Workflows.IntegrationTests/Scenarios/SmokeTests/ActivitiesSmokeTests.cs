using Elsa.Testing.Shared;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.SmokeTests;

/// <summary>
/// Smoke tests that verify basic functionality of core workflow activities.
/// </summary>
public class ActivitiesSmokeTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    [Test]
    [DisplayName("Smoke test executes all core activities successfully")]
    public async Task SmokeTest_ExecutesAllActivities_Successfully()
    {
        // Act
        var (result, _) = await RunWorkflowAndCaptureOutput();

        // Assert - Workflow completed successfully
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);

        // Verify outputs were set correctly
        var outputs = result.WorkflowState.Output;
        await Assert.That(outputs).IsNotNull();
        await Assert.That(outputs.TryGetValue("FinalResult", out var finalResultObj)).IsTrue();

        var finalResult = finalResultObj?.ToString();
        await Assert.That(finalResult).IsNotNull();
        await Assert.That(finalResult).Contains("Counter=10", StringComparison.CurrentCulture);
        await Assert.That(finalResult).Contains("Name=Updated Name", StringComparison.CurrentCulture);
        await Assert.That(finalResult).Contains("Result=Switch-2", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("Smoke test verifies all activities were executed")]
    public async Task SmokeTest_VerifiesActivityExecution()
    {
        // Act
        var (_, lines) = await RunWorkflowAndCaptureOutput();

        // Assert - Verify key activities executed by checking WriteLine outputs
        await Assert.That(lines).Contains(line => line.Contains("=== Smoke Test Started ==="));
        await Assert.That(lines).Contains(line => line.Contains("Name: Updated Name"));
        await Assert.That(lines).Contains(line => line.Contains("Untyped: Untyped value"));
        await Assert.That(lines).Contains(line => line.Contains("If branch: True path executed"));
        await Assert.That(lines).Contains(line => line.Contains("Switch: Case 2 executed"));
        await Assert.That(lines).Contains(line => line.Contains("For loop: Completed with 3 iterations"));
        await Assert.That(lines).Contains(line => line.Contains("While loop: Completed with 3 iterations"));
        await Assert.That(lines).Contains(line => line.Contains("ForEach loop: Completed with 2 items processed"));

        // Verify activities after Complete did NOT execute
        await Assert.That(lines).DoesNotContain(line => line.Contains("After Complete (should not execute)"));
    }

    [Test]
    [DisplayName("Break activity works correctly in different loop contexts")]
    public async Task SmokeTest_BreakActivity_WorksInDifferentLoops()
    {
        // Act
        var (_, lines) = await RunWorkflowAndCaptureOutput();

        // Assert - Verify Break worked in For loop (stopped at 3 iterations, not 100)
        await Assert.That(lines).Contains(line => line.Contains("For loop: Completed with 3 iterations"));

        // Verify Break worked in While loop (stopped at 3 iterations, didn't run infinitely)
        await Assert.That(lines).Contains(line => line.Contains("While loop: Iteration 1"));
        await Assert.That(lines).Contains(line => line.Contains("While loop: Iteration 2"));
        await Assert.That(lines).Contains(line => line.Contains("While loop: Iteration 3"));
        await Assert.That(lines).Contains(line => line.Contains("While loop: Completed with 3 iterations"));

        // Verify Break worked in ForEach (stopped after 2 items: A, B, not C)
        await Assert.That(lines).Contains(line => line.Contains("ForEach: Item 'A'"));
        await Assert.That(lines).Contains(line => line.Contains("ForEach: Item 'B'"));
        await Assert.That(lines).DoesNotContain(line => line.Contains("ForEach: Item 'C'"));
        await Assert.That(lines).Contains(line => line.Contains("ForEach loop: Completed with 2 items processed"));
    }

    [Test]
    [DisplayName("Switch activity executes correct case")]
    public async Task SmokeTest_SwitchActivity_ExecutesCorrectCase()
    {
        // Act
        var (_, lines) = await RunWorkflowAndCaptureOutput();

        // Assert - Only Case 2 should execute
        await Assert.That(lines).DoesNotContain(line => line.Contains("Switch: Case 1 (should not execute)"));
        await Assert.That(lines).Contains(line => line.Contains("Switch: Case 2 executed"));
        await Assert.That(lines).DoesNotContain(line => line.Contains("Switch: Case 3 (should not execute)"));
        await Assert.That(lines).DoesNotContain(line => line.Contains("Switch: Default (should not execute)"));
    }

    [Test]
    [DisplayName("If activity executes correct branch")]
    public async Task SmokeTest_IfActivity_ExecutesCorrectBranch()
    {
        // Act
        var (_, lines) = await RunWorkflowAndCaptureOutput();

        // Assert - Only Then branch should execute
        await Assert.That(lines).Contains(line => line.Contains("If branch: True path executed"));
        await Assert.That(lines).DoesNotContain(line => line.Contains("If branch: False path (should not execute)"));
    }
    
    private async Task<(RunWorkflowResult Result, List<string> Lines)> RunWorkflowAndCaptureOutput()
    {
        var workflow = new ActivitiesSmokeTestWorkflow();
        var result = await _fixture.RunWorkflowAsync(workflow);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        return (result, lines);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_fixture);
}
