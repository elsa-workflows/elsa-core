using Elsa.Testing.Shared.Services;
using Elsa.Workflows.Attributes;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowInstanceDeletion.Activities;

/// <summary>
/// A custom activity that simulates a long-running operation to test workflow deletion during execution.
/// This activity sends a signal when it starts, then delays for a specified duration.
/// </summary>
[Activity("Elsa.Workflows.ComponentTests", "LongRunningActivity", "A long-running activity for testing")]
public class LongRunningActivity : CodeActivity
{
    /// <summary>
    /// The duration to delay for (in milliseconds)
    /// </summary>
    public int DelayMilliseconds { get; set; } = 3000;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var signalManager = context.GetRequiredService<SignalManager>();
        var workflowInstanceId = context.WorkflowExecutionContext.Id;

        // Send a signal to notify the test that the activity has started
        signalManager.Trigger("workflow-started", workflowInstanceId);

        // Simulate a long-running operation by using Task.Delay
        // This keeps the workflow in memory and executing
        await Task.Delay(DelayMilliseconds, context.CancellationToken);
    }
}
