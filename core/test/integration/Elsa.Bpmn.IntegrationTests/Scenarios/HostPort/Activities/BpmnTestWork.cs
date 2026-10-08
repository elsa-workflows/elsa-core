using Elsa.Workflows;
using Elsa.Workflows.Signals;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;

/// <summary>
/// A stand-in for BPMN work that finishes as soon as it runs.
/// </summary>
public class BpmnTestWork : CodeActivity
{
    /// <inheritdoc />
    public BpmnTestWork() => OnSignalReceived<CancelSignal>(OnCancelled);

    /// <summary>The log to record into.</summary>
    public BpmnTestLog? Log { get; set; }

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context) => BpmnTestWorkProbe.RecordExecution(this, Log, context);

    private void OnCancelled(CancelSignal signal, SignalContext context) => BpmnTestWorkProbe.RecordCancellation(this, Log, context);
}
