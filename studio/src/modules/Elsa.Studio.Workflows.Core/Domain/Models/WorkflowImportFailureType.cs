namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Defines the workflow import failure type enumeration.
/// </summary>
public enum WorkflowImportFailureType
{
    Exception,
    InvalidSchema,

    /// <summary>
    /// A BPMN import was refused because this deployment does not declare a BPMN host capability the document
    /// requires (a <c>422</c> from <c>bpmn/import</c>). The refusal's capability names and offending element ids
    /// are shown in a dialog rather than a snackbar; see <see cref="Elsa.Studio.Workflows.Domain.Models.Bpmn.BpmnCapabilityRefusal"/>.
    /// </summary>
    CapabilityRefusal
}