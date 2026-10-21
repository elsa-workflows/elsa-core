using Elsa.Workflows;
using Elsa.Workflows.Exceptions;
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

    /// <summary>
    /// The code to fail with, as Elsa's <c>Fault</c> activity does, or <c>null</c> to fail with an ordinary exception,
    /// which names no BPMN error.
    /// </summary>
    public string? Code { get; set; }

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        BpmnTestWorkProbe.RecordExecution(this, Log, context);
        if (Code is not null)
            throw new FaultException(Code, "Test", "Business", $"The work bound to '{Id}' failed with '{Code}'.");

        throw new InvalidOperationException($"The work bound to '{Id}' failed.");
    }

    private void OnCancelled(CancelSignal signal, SignalContext context) => BpmnTestWorkProbe.RecordCancellation(this, Log, context);
}
