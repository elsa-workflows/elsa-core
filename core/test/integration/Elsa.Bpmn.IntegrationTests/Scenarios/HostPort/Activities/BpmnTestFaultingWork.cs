using Elsa.Workflows;
using Elsa.Workflows.Signals;

namespace Elsa.Bpmn.IntegrationTests.Scenarios.HostPort.Activities;

/// <summary>
/// A stand-in for BPMN work that fails. The host reports that work failed and describes why in human terms; which
/// BPMN error a catcher matches is a property of the model, not of the report.
/// </summary>
public class BpmnTestFaultingWork : CodeActivity
{
    /// <inheritdoc />
    public BpmnTestFaultingWork() => OnSignalReceived<CancelSignal>(OnCancelled);

    /// <summary>The log to record into.</summary>
    public BpmnTestLog? Log { get; set; }

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        BpmnTestWorkProbe.RecordExecution(this, Log, context);
        throw new InvalidOperationException($"The work bound to '{Id}' failed.");
    }

    private void OnCancelled(CancelSignal signal, SignalContext context) => BpmnTestWorkProbe.RecordCancellation(this, Log, context);
}
