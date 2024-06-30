using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.OrchardCore.WebhookPayloads;
using Elsa.Webhooks.Stimuli;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace Elsa.OrchardCore.Activities;

public class ContentItemPublished : Trigger<ContentItemPublishedPayload>, INotification
{
    /// The content type to handle the event for.
    [Input(Description = "The content type to handle the event for.")]
    public string ContentType { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (!context.IsTriggerOfWorkflow())
        {
            context.CreateBookmark(GetStimulus(context.ExpressionExecutionContext), OnResumeAsync);
            return;
        }

        await ExecuteInternalAsync(context);
    }

    protected override object GetTriggerPayload(TriggerIndexingContext context) => new WebhookEventReceivedStimulus(ContentType);
    private async ValueTask OnResumeAsync(ActivityExecutionContext context) => await ExecuteInternalAsync(context);
    private object GetStimulus(ExpressionExecutionContext context) => new WebhookEventReceivedStimulus(ContentType);

    private async Task ExecuteInternalAsync(ActivityExecutionContext context)
    {
        var payload = context.GetWorkflowInput<ContentItemPublishedPayload>();
        context.SetResult(payload);
        await context.CompleteActivityAsync();
    }
}