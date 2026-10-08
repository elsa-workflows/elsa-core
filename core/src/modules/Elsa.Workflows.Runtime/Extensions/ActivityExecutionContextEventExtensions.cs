using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Stimuli;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ActivityExecutionContextEventExtensions
{
    /// <summary>
    /// Suspends the current activity's execution and waits for a specified event to occur before continuing.
    /// </summary>
    /// <param name="context">The activity execution context associated with the current activity execution.</param>
    /// <param name="eventName">The name of the event to wait for.</param>
    /// <param name="callback">An optional delegate to execute when the event occurs.</param>
    public static void WaitForEvent(this ActivityExecutionContext context, string eventName, ExecuteActivityDelegate? callback = null)
    {
        if (context.IsTriggerOfWorkflow())
        {
            callback?.Invoke(context);
            return;
        }

        var options = new CreateBookmarkArgs
        {
            Stimulus = new EventStimulus(eventName),
            IncludeActivityInstanceId = false,
            BookmarkName = RuntimeStimulusNames.Event,
            Callback = callback
        };
        context.CreateBookmark(options);
    }
    
    public static EventStimulus GetEventStimulus(this TriggerIndexingContext context, string eventName)
    {
        context.TriggerName = RuntimeStimulusNames.Event;
        return new(eventName);
    }

    public static object? GetEventInput(this ActivityExecutionContext context)
    {
        return context.TryGetWorkflowInput<object?>(Event.EventInputWorkflowInputKey, out var input) ? input : null;
    }
}