using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <inheritdoc />
[FlowNode("Connected", "Disconnected")]
[PublicAPI]
public class FlowAnswerCall : AnswerCallBase
{
    /// <inheritdoc />
    [JsonConstructor]
    public FlowAnswerCall()
    {
    }
    
    /// <inheritdoc />
    public FlowAnswerCall([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected override async ValueTask HandleConnectedAsync(ActivityExecutionContext context) => await context.CompleteActivityAsync(new Outcomes("Connected"));

    /// <inheritdoc />
    protected override async ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => await context.CompleteActivityAsync(new Outcomes("Disconnected"));
}

/// <inheritdoc />
[PublicAPI]
public class AnswerCall : AnswerCallBase
{
    /// <inheritdoc />
    [JsonConstructor]
    public AnswerCall()
    {
    }
    
    /// <inheritdoc />
    public AnswerCall([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The activity to schedule when the call was successfully answered.
    /// </summary>
    [Port]
    public IActivity? Connected { get; set; }

    /// <summary>
    /// The activity to schedule when the call was no longer active.
    /// </summary>
    [Port]
    public IActivity? Disconnected { get; set; }

    /// <inheritdoc />
    protected override async ValueTask HandleConnectedAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Connected);

    /// <inheritdoc />
    protected override async ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Disconnected);
}

/// <summary>
/// Answer an incoming call. You must issue this command before executing subsequent commands on an incoming call.
/// </summary>
[Activity(Constants.Namespace, "Answer an incoming call. You must issue this command before executing subsequent commands on an incoming call.", Kind = ActivityKind.Task)]
[WebhookDriven(WebhookEventTypes.CallAnswered)]
[PublicAPI]
public abstract class AnswerCallBase : Activity<CallAnsweredPayload>, IBookmarksPersistedHandler
{
    /// <inheritdoc />
    protected AnswerCallBase(string? source = default, int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// The call control ID to answer. Leave blank when the workflow is driven by an incoming call and you wish to pick up that one.
    /// </summary>
    public Input<string?>? CallControlId { get; set; }

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        // Create a bookmark first, then after it's persisted, we call out to Telnyx.
        // This ensures that the bookmark is available in case Telnyx responds with the webhook before the runtime got a chance to persist bookmarks.
        context.CreateBookmark(new WebhookEventBookmarkPayload(WebhookEventTypes.CallAnswered), ResumeAsync);
    }

    /// <summary>
    /// Invokes Telnyx to answer the call.
    /// </summary>
    public async ValueTask BookmarksPersistedAsync(ActivityExecutionContext context) => await InvokeTelnyxAsync(context);

    /// <summary>
    /// Invoked when the call was successfully answered.
    /// </summary>
    protected abstract ValueTask HandleConnectedAsync(ActivityExecutionContext context);
    
    /// <summary>
    /// Invoked when the call was no longer active.
    /// </summary>
    protected abstract ValueTask HandleDisconnectedAsync(ActivityExecutionContext context);

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var payload = context.GetInput<CallAnsweredPayload>();
        context.Set(Result, payload);
        await HandleConnectedAsync(context);
    }

    /// <summary>
    /// Invokes Telnyx' API to answer the call.
    /// </summary>
    private async ValueTask InvokeTelnyxAsync(ActivityExecutionContext context)
    {
        var callControlId = context.GetPrimaryCallControlId(CallControlId) ?? throw new Exception("CallControlId is required.");
        
        var request = new AnswerCallRequest
        {
            ClientState = context.CreateCorrelatingClientState()
        };
        
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.AnswerCallAsync(callControlId, request, context.CancellationToken);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await HandleDisconnectedAsync(context);
        }
    }
}