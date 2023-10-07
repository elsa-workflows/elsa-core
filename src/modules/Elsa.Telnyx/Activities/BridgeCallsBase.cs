using Elsa.Extensions;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Models;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Bridge two calls.
/// </summary>
[Activity(Constants.Namespace, "Bridge two calls.", Kind = ActivityKind.Task)]
[PublicAPI]
public abstract class BridgeCallsBase : Activity<BridgedCallsOutput>
{
    /// <inheritdoc />
    protected BridgeCallsBase(string? source = default, int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The source call control ID of one of the call to bridge with. Leave empty to use the ambient inbound call control Id, if there is one.
    /// </summary>
    [Input(DisplayName = "Call Control ID A", Description = "The source call control ID of one of the call to bridge with. Leave empty to use the ambient inbound call control Id, if there is one.")]
    public Input<string> CallControlIdA { get; set; } = default!;

    /// <summary>
    /// The destination call control ID of the call you want to bridge with.
    /// </summary>
    [Input(DisplayName = "Call Control ID B", Description = "The destination call control ID of the call you want to bridge with.")]
    public Input<string> CallControlIdB { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var callControlIdA = CallControlIdA.Get(context);
        var callControlIdB = CallControlIdB.Get(context);
        var request = new BridgeCallsRequest(callControlIdB, ClientState: context.CreateCorrelatingClientState(context.Id));
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.BridgeCallsAsync(callControlIdA, request, context.CancellationToken);
            
            var bookmarkA = new WebhookEventBookmarkPayload(WebhookEventTypes.CallBridged, callControlIdA);
            var bookmarkB = new WebhookEventBookmarkPayload(WebhookEventTypes.CallBridged, callControlIdB);
            context.CreateBookmarks(new[] { bookmarkA, bookmarkB }, ResumeAsync, false);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;

            await HandleDisconnectedAsync(context);
        }
    }

    /// <summary>
    /// Called when the call is disconnected.
    /// </summary>
    protected abstract ValueTask HandleDisconnectedAsync(ActivityExecutionContext context);
    
    /// <summary>
    /// Called when the call is bridged.
    /// </summary> <param name="context"></param>
    /// <returns></returns>
    protected abstract ValueTask HandleBridgedAsync(ActivityExecutionContext context);
    
    /// <summary>
    /// Called when the activity is completed.
    /// </summary>
    protected async ValueTask OnCompleted(ActivityCompletedContext context) => await context.TargetContext.CompleteActivityAsync();

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var payload = context.GetWorkflowInput<CallBridgedPayload>()!;
        var callControlIdA = CallControlIdA.Get(context);
        var callControlIdB = CallControlIdB.Get(context);;

        if (payload.CallControlId == callControlIdA) context.SetProperty("CallBridgedPayloadA", payload);
        if (payload.CallControlId == callControlIdB) context.SetProperty("CallBridgedPayloadB", payload);

        var callBridgedPayloadA = context.GetProperty<CallBridgedPayload>("CallBridgedPayloadA");
        var callBridgedPayloadB = context.GetProperty<CallBridgedPayload>("CallBridgedPayloadB");

        if (callBridgedPayloadA != null && callBridgedPayloadB != null)
        {
            context.Set(Result, new BridgedCallsOutput(callBridgedPayloadA, callBridgedPayloadB));
            await HandleBridgedAsync(context);
        }
    }
}