using System.Xml;
using Bpmn.Model;
using Bpmn.Semantics;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Exceptions;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// Resolves the event-defined start event a trigger-started root <see cref="BpmnProcess"/> begins at.
/// </summary>
/// <remarks>
/// <para>
/// The interpreter begins at every plain start event unless the host names the event-defined start event an external
/// trigger matched. The workflow runtime starts a trigger-bound workflow on the activity that registered the trigger,
/// but it does not say which of that activity's trigger payloads matched, so the start event is recovered from what
/// the runtime does pass: an event stimulus always carries <c>Event.EventInputWorkflowInputKey</c> in the workflow
/// input, and a scheduled timer start never does.
/// </para>
/// <para>
/// Within that kind, start events that resolve to the same stimulus are interchangeable, exactly as
/// <see cref="BpmnProcess"/> collapses them into one trigger. When start events of the same kind resolve to different
/// stimuli, the trigger that fired cannot be told apart from here, and the process faults rather than begin at a start
/// event the trigger may not have matched.
/// </para>
/// </remarks>
internal static class BpmnStartEventResolver
{
    /// <summary>
    /// The fault code raised when more than one start event could have matched the trigger that started the process.
    /// </summary>
    public const string AmbiguousTriggerFaultCode = "elsa.bpmn.start.ambiguous-trigger";

    /// <summary>
    /// The start signal for a process started by one of its own triggers, or <c>null</c> when none of its start events
    /// registers a trigger of that kind, in which case the interpreter falls back to its plain start events.
    /// </summary>
    /// <param name="process">The process the trigger started.</param>
    /// <param name="startedByEvent">Whether an event stimulus, rather than a timer, started the process.</param>
    /// <exception cref="BpmnScopeFaultException">More than one distinct stimulus of the trigger's kind is registered.</exception>
    public static BpmnStartSignal? Resolve(BpmnProcessDefinition process, bool startedByEvent)
    {
        var candidates = (
            from element in process.Elements
            where string.Equals(element.ElementType, BpmnElementTypes.StartEvent, StringComparison.Ordinal)
            from eventDefinition in element.EventDefinitions
            let stimulus = startedByEvent ? EventStimulusOf(eventDefinition) : TimerStimulusOf(eventDefinition)
            where stimulus is not null
            select (element.ElementId, Stimulus: stimulus)).ToList();

        if (candidates.Count == 0)
            return null;

        if (candidates.Select(candidate => candidate.Stimulus).Distinct(StringComparer.Ordinal).Count() > 1)
        {
            var kind = startedByEvent ? "message or signal" : "timer";
            var elementIds = string.Join(", ", candidates.Select(candidate => $"'{candidate.ElementId}'").Distinct(StringComparer.Ordinal));

            throw new BpmnScopeFaultException(
                AmbiguousTriggerFaultCode,
                $"BPMN process '{process.ProcessId}' was started by a {kind} trigger, but its start events {elementIds} register different {kind} triggers "
                + "and the workflow runtime does not report which one fired. Model each of these start events as its own process.");
        }

        return new BpmnStartSignal(candidates[0].ElementId);
    }

    private static string? EventStimulusOf(BpmnEventDefinition eventDefinition) =>
        eventDefinition.Type is BpmnEventDefinitionTypes.Message or BpmnEventDefinitionTypes.Signal
        && eventDefinition.Properties.TryGetValue(BpmnEventDefinitionProperties.Name, out var name)
        && !string.IsNullOrWhiteSpace(name)
            ? name
            : null;

    // Mirrors what BpmnProcess registers: an interval it would refuse never fires, so it cannot be the start that did.
    private static string? TimerStimulusOf(BpmnEventDefinition eventDefinition)
    {
        if (!string.Equals(eventDefinition.Type, BpmnEventDefinitionTypes.Timer, StringComparison.Ordinal))
            return null;

        if (eventDefinition.Properties.TryGetValue(BpmnEventDefinitionProperties.Interval, out var isoInterval))
            return TryParseInterval(isoInterval, out var interval) && interval >= BpmnProcess.MinimumTimerInterval ? $"interval:{interval}" : null;

        return eventDefinition.Properties.TryGetValue(BpmnEventDefinitionProperties.Cron, out var cron) ? $"cron:{cron}" : null;
    }

    private static bool TryParseInterval(string isoInterval, out TimeSpan interval)
    {
        try
        {
            interval = XmlConvert.ToTimeSpan(isoInterval);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentNullException)
        {
            interval = TimeSpan.Zero;
            return false;
        }
    }
}
