namespace Elsa.Bpmn.Interchange.Exceptions;

/// <summary>
/// Thrown when a BPMN process, or a subprocess body it can reach, cannot run: building its graph refuses it.
/// </summary>
/// <remarks>
/// Import builds the graph of the process and of every body it can reach, as the runtime does when a run starts, so a
/// process that could never run is refused before it is persisted rather than when it first runs. The message is
/// <c>Bpmn.Semantics</c>' own, naming the elements, and <see cref="Exception.InnerException"/> is its exception.
/// </remarks>
public class BpmnProcessInvalidException(string message, Exception innerException) : Exception(message, innerException);
