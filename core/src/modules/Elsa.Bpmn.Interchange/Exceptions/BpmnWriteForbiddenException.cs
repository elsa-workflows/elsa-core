namespace Elsa.Bpmn.Interchange.Exceptions;

internal sealed class BpmnWriteForbiddenException()
    : InvalidOperationException("Workflow editing is forbidden in the current management mode or for this definition.");
