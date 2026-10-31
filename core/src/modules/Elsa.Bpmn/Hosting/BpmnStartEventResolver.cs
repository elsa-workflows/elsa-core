using System.Xml;
using Bpmn.Model;
using Bpmn.Semantics;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Exceptions;
using Elsa.Scheduling;
using Elsa.Scheduling.Bookmarks;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// Resolves the event-defined start event a trigger-started root <see cref="BpmnProcess"/> begins at.
/// </summary>
/// <remarks>
/// <para>
/// The interpreter begins at every plain start event unless the host names the event-defined start event an external
/// trigger matched. The workflow runtime starts a trigger-bound workflow on the activity that registered the trigger
/// and records the stimulus hash of the stored trigger that matched (<see cref="TriggerWorkflowProperties.StimulusHash"/>).
/// <see cref="ResolveByStimulusHashAsync"/> finds the start event that registered exactly that trigger.
/// </para>
/// <para>
/// A run that carries no such hash (one started directly with this process as its trigger activity, or one dispatched
/// before the runtime recorded the hash) falls back to <see cref="Resolve"/>, which recovers the start event from the
/// trigger kind alone: an event stimulus always carries <c>Event.EventInputWorkflowInputKey</c> in the workflow input,
/// and a scheduled timer start never does. Within that kind, start events that resolve to the same stimulus are
/// interchangeable, exactly as <see cref="BpmnProcess"/> collapses them into one trigger. When start events of the same
/// kind resolve to different stimuli, the trigger that fired cannot be told apart that way, and the process faults
/// rather than begin at a start event the trigger may not have matched.
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
            from element in StartEventsOf(process)
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
                + "and the run does not identify the stored trigger that fired. Start it through the workflow runtime, which records that trigger, "
                + "or model each of these start events as its own process.");
        }

        return new BpmnStartSignal(candidates[0].ElementId);
    }

    /// <summary>
    /// The start signal for the start event that registered the stored trigger with the given stimulus hash, or
    /// <c>null</c> when none of the process's start events did.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Message, signal and cron starts are matched by hashing the same stimulus <see cref="BpmnProcess"/> registers for
    /// them. An interval timer cannot be: the <see cref="TimerTriggerPayload"/> it registers carries the
    /// <see cref="TimerTriggerPayload.StartAt"/> the indexer stamped when it stored the trigger, and that instant is part
    /// of the hash. Its interval is therefore read back from the stored trigger itself, through
    /// <paramref name="findTimerInterval"/>, and only when the process has an interval timer start to match.
    /// </para>
    /// <para>
    /// Start events that register the same stimulus are one trigger, so the first of them in document order is the
    /// start, exactly as <see cref="Resolve"/> picks.
    /// </para>
    /// </remarks>
    /// <param name="process">The process the trigger started.</param>
    /// <param name="stimulusHash">The stimulus hash of the stored trigger that started the process.</param>
    /// <param name="stimulusHasher">The hasher the trigger indexer used.</param>
    /// <param name="findTimerInterval">Reads the interval of the stored timer trigger with <paramref name="stimulusHash"/>, if there is one.</param>
    public static async ValueTask<BpmnStartSignal?> ResolveByStimulusHashAsync(
        BpmnProcessDefinition process,
        string stimulusHash,
        IStimulusHasher stimulusHasher,
        Func<ValueTask<TimeSpan?>> findTimerInterval)
    {
        var intervalStarts = new List<(string ElementId, TimeSpan Interval)>();

        foreach (var element in StartEventsOf(process))
        {
            foreach (var eventDefinition in element.EventDefinitions)
            {
                if (EventStimulusOf(eventDefinition) is { } eventName
                    && HashEquals(stimulusHasher.Hash(RuntimeStimulusNames.Event, new EventStimulus(eventName)), stimulusHash))
                {
                    return new BpmnStartSignal(element.ElementId);
                }

                if (CronOf(eventDefinition) is { } cron
                    && HashEquals(stimulusHasher.Hash(SchedulingStimulusNames.Cron, new CronTriggerPayload(cron)), stimulusHash))
                {
                    return new BpmnStartSignal(element.ElementId);
                }

                if (IntervalOf(eventDefinition) is { } interval)
                {
                    intervalStarts.Add((element.ElementId, interval));
                }
            }
        }

        if (intervalStarts.Count == 0 || await findTimerInterval() is not { } triggeredInterval)
        {
            return null;
        }

        var match = intervalStarts.FirstOrDefault(start => start.Interval == triggeredInterval);
        return match.ElementId is null ? null : new BpmnStartSignal(match.ElementId);
    }

    private static IEnumerable<BpmnElement> StartEventsOf(BpmnProcessDefinition process) =>
        process.Elements.Where(element => string.Equals(element.ElementType, BpmnElementTypes.StartEvent, StringComparison.Ordinal));

    private static bool HashEquals(string candidate, string stimulusHash) => string.Equals(candidate, stimulusHash, StringComparison.Ordinal);

    private static string? EventStimulusOf(BpmnEventDefinition eventDefinition) =>
        eventDefinition.Type is BpmnEventDefinitionTypes.Message or BpmnEventDefinitionTypes.Signal
        && eventDefinition.Properties.TryGetValue(BpmnEventDefinitionProperties.Name, out var name)
        && !string.IsNullOrWhiteSpace(name)
            ? name
            : null;

    private static string? TimerStimulusOf(BpmnEventDefinition eventDefinition) =>
        IntervalOf(eventDefinition) is { } interval ? $"interval:{interval}"
        : CronOf(eventDefinition) is { } cron ? $"cron:{cron}"
        : null;

    // Mirrors what BpmnProcess registers: an interval it would refuse never fires, so it cannot be the start that did.
    private static TimeSpan? IntervalOf(BpmnEventDefinition eventDefinition) =>
        IsTimer(eventDefinition)
        && eventDefinition.Properties.TryGetValue(BpmnEventDefinitionProperties.Interval, out var isoInterval)
        && TryParseInterval(isoInterval, out var interval)
        && interval >= BpmnProcess.MinimumTimerInterval
            ? interval
            : null;

    // A timer that declares an interval registers that interval or nothing, never a cron, exactly as BpmnProcess does.
    private static string? CronOf(BpmnEventDefinition eventDefinition) =>
        IsTimer(eventDefinition)
        && !eventDefinition.Properties.ContainsKey(BpmnEventDefinitionProperties.Interval)
        && eventDefinition.Properties.TryGetValue(BpmnEventDefinitionProperties.Cron, out var cron)
            ? cron
            : null;

    private static bool IsTimer(BpmnEventDefinition eventDefinition) =>
        string.Equals(eventDefinition.Type, BpmnEventDefinitionTypes.Timer, StringComparison.Ordinal);

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
