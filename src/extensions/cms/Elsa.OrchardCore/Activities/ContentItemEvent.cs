using Elsa.Extensions;
using Elsa.OrchardCore.Stimuli;
using Elsa.OrchardCore.WebhookPayloads;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace Elsa.OrchardCore.Activities;

/// <summary>
/// Represents a trigger for handling Orchard Core content item events in workflows.
/// This class is derived from the <see cref="Trigger{T}"/> base class and is specifically tailored
/// to react to events associated with content items, such as creation, modification, or deletion.
/// </summary>
public class ContentItemEvent : Trigger<ContentItemEventPayload>, INotification
{
    /// <summary>
    /// The content type to handle the event for.
    /// </summary>
    [Input(Description = "The content type to handle the event for.")]
    public string ContentType { get; set; } = null!;
    
    /// <summary>
    /// The event to handle.
    /// </summary>
    [Input(Description = "The event to handle the event for.")]
    public string EventType { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (!context.IsTriggerOfWorkflow())
        {
            context.CreateBookmark(GetStimulus(), OnResumeAsync);
            return;
        }

        await ExecuteInternalAsync(context);
    }

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context) => new ContentItemEventStimulus(ContentType, EventType);
    private async ValueTask OnResumeAsync(ActivityExecutionContext context) => await ExecuteInternalAsync(context);
    private object GetStimulus() => new ContentItemEventStimulus(ContentType, EventType);

    private async Task ExecuteInternalAsync(ActivityExecutionContext context)
    {
        var payload = context.GetWorkflowInput<ContentItemEventPayload>();
        context.SetResult(payload);
        await context.CompleteActivityAsync();
    }
}