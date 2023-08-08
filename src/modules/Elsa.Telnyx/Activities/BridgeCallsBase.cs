using Elsa.Extensions;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Models;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Bridge two calls.
/// </summary>
[Activity(Constants.Namespace, "Bridge two calls.", Kind = ActivityKind.Task)]
[WebhookDriven(WebhookEventTypes.CallBridged)]
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
    public Input<string?>? CallControlIdA { get; set; }

    /// <summary>
    /// The destination call control ID of the call you want to bridge with.
    /// </summary>
    [Input(DisplayName = "Call Control ID B", Description = "The destination call control ID of the call you want to bridge with.")]
    public Input<string?>? CallControlIdB { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var callControlIdA = context.GetPrimaryCallControlId(CallControlIdA) ?? throw new Exception("CallControlA is required");
        var callControlIdB = context.GetSecondaryCallControlId(CallControlIdB) ?? throw new Exception("CallControlB is required");
        var request = new BridgeCallsRequest(callControlIdB, ClientState: context.CreateCorrelatingClientState());
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.BridgeCallsAsync(callControlIdA, request, context.CancellationToken);
            
            var bookmarkA = new WebhookEventBookmarkPayload(WebhookEventTypes.CallBridged, callControlIdA);
            var bookmarkB = new WebhookEventBookmarkPayload(WebhookEventTypes.CallBridged, callControlIdB);
            context.CreateBookmarks(new[] { bookmarkA, bookmarkB }, ResumeAsync);
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
    protected async ValueTask OnCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext) => await context.CompleteActivityAsync();

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var payload = context.GetInput<CallBridgedPayload>()!;
        var callControlIdA = context.GetPrimaryCallControlId(CallControlIdA);
        var callControlIdB = context.GetSecondaryCallControlId(CallControlIdB);

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