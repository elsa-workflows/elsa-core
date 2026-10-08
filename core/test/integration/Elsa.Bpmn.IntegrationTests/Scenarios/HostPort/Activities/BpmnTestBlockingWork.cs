using Elsa.Workflows;
using Elsa.Workflows.Signals;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;

/// <summary>
/// A stand-in for BPMN work that runs until something else finishes it — a long-running task, or an armed listener
/// waiting for its trigger. It blocks on a bookmark, so a test resumes it by name.
/// </summary>
public class BpmnTestBlockingWork : Activity
{
    /// <inheritdoc />
    public BpmnTestBlockingWork() => OnSignalReceived<CancelSignal>(OnCancelled);

    /// <summary>The log to record into.</summary>
    public BpmnTestLog? Log { get; set; }

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        BpmnTestWorkProbe.RecordExecution(this, Log, context);
        context.CreateBookmark(ResumeAsync);
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        Log?.Record($"resumed:{Id}");
        await context.CompleteActivityAsync();
    }

    private void OnCancelled(CancelSignal signal, SignalContext context) => BpmnTestWorkProbe.RecordCancellation(this, Log, context);
}
