using Elsa.Scheduling.Activities;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.CronActivity;

/// <summary>
/// Verifies that a <see cref="Cron"/> activity with a blank expression (e.g. a JavaScript expression that
/// resolves to an empty string at runtime) completes without throwing, instead of failing with a
/// CronFormatException, mirroring the "blank means disabled" behavior used during trigger indexing.
/// </summary>
public class BlankCronExecutionTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Theory(DisplayName = "Blank Cron expression completes the activity without scheduling")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankCronExpression_CompletesAndContinues(string expression)
    {
        var root = new Sequence
        {
            Activities =
            {
                new WriteLine("before"),
                new Cron { CronExpression = new Input<string>(expression) },
                new WriteLine("after")
            }
        };

        var result = await _fixture.RunActivityAsync(root);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();

        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Contains(lines, line => line.Contains("before"));
        Assert.Contains(lines, line => line.Contains("after"));
    }
}
