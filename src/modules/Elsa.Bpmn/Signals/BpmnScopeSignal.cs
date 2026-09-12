using System.Text.Json;

namespace Elsa.Bpmn.Signals;

/// <summary>
/// Carries the interpreter's <c>SignalEnclosingScope</c> command from a nested BPMN scope to the scope that started it.
/// </summary>
/// <remarks>
/// The signal travels the ordinary Elsa channel, so it is delivered to the sending scope first and then to each
/// ancestor in turn. Only the scope that started the sending scope's unit of work claims it; every other receiver
/// lets it keep bubbling, which is also how a scope that cannot match an escalation re-signals it one hop further
/// out. There is deliberately one code per kind of scope signal rather than a growing namespace: BPMN escalation
/// travels under <c>BpmnInterpreter.EscalationSignalCode</c> with its identity in the payload.
/// </remarks>
/// <param name="Code">The signal code.</param>
/// <param name="Payload">The signal payload.</param>
public record BpmnScopeSignal(string Code, JsonElement? Payload);
