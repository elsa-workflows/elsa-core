using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using JetBrains.Annotations;

namespace Elsa.Bpmn.Interchange.Activities;

/// <summary>
/// Stands in for a BPMN task that the document describes but nothing binds to an Elsa activity yet, so a process can be
/// imported and saved before every task has been given its implementation.
/// </summary>
/// <remarks>
/// Publishing refuses a process while any of these remain (<c>ValidateBpmnProcessBindings</c>), so a published workflow
/// never contains one. Running one anyway, such as from a draft, faults: carrying on would let the process continue
/// past work that never happened.
/// </remarks>
[Browsable(false)]
[Activity("Elsa", "BPMN", "Stands in for a BPMN task that is not bound to an Elsa activity yet. Faults if it runs.")]
[PublicAPI]
public class BpmnUnboundTask : CodeActivity
{
    /// <inheritdoc />
    public BpmnUnboundTask([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>The id of the BPMN element this activity stands in for.</summary>
    public string ElementId { get; set; } = null!;

    /// <summary>The BPMN element type of that element, such as <c>serviceTask</c>.</summary>
    public string TaskType { get; set; } = null!;

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        throw new InvalidOperationException($"BPMN element '{ElementId}' ({TaskType}) is not bound to an Elsa activity, so there is nothing to run. Bind it to an activity before running the process.");
    }
}
