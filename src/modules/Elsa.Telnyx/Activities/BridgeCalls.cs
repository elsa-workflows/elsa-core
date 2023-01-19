using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Services;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <inheritdoc />
[FlowNode("Bridged", "Disconnected")]
public class FlowBridgeCalls : BridgeCallsBase
{
    /// <inheritdoc />
    [JsonConstructor]
    public FlowBridgeCalls([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    protected override ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => context.CompleteActivityAsync("Disconnected");
    protected override ValueTask HandleBridgedAsync(ActivityExecutionContext context) => context.CompleteActivityAsync("Bridged");
}

/// <inheritdoc />
public class BridgeCalls : BridgeCallsBase
{
    /// <inheritdoc />
    [JsonConstructor]
    public BridgeCalls([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// The <see cref="IActivity"/> to execute when the source leg call is no longer active.
    /// </summary>
    [Port]public IActivity? Disconnected { get; set; }
    
    /// <summary>
    /// The <see cref="IActivity"/> to execute when the two calls are bridged.
    /// </summary>
    [Port]public IActivity? Bridged { get; set; }

    /// <inheritdoc />
    protected override async ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Disconnected, OnCompleted);

    /// <inheritdoc />
    protected override async ValueTask HandleBridgedAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Bridged, OnCompleted);
}

/// <summary>
/// Bridge two calls.
/// </summary>
[Activity(Constants.Namespace, "Bridge two calls.", Kind = ActivityKind.Task)]
public abstract class BridgeCallsBase : Activity<BridgedCallsOutput>, IBookmarksPersistedHandler
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
    public async ValueTask BookmarksPersistedAsync(ActivityExecutionContext context)
    {
        var callControlIdA = context.GetPrimaryCallControlId(CallControlIdA) ?? throw new Exception("CallControlA is required");
        var callControlIdB = context.GetSecondaryCallControlId(CallControlIdB) ?? throw new Exception("CallControlB is required");
        var request = new BridgeCallsRequest(callControlIdB, ClientState: context.CreateCorrelatingClientState());
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.BridgeCallsAsync(callControlIdA, request, context.CancellationToken);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;

            await HandleDisconnectedAsync(context);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var callControlIdA = context.GetPrimaryCallControlId(CallControlIdA) ?? throw new Exception("CallControlA is required");
        var callControlIdB = context.GetSecondaryCallControlId(CallControlIdB) ?? throw new Exception("CallControlB is required");
        var bookmarkA = new CallBridgedBookmarkPayload(callControlIdA);
        var bookmarkB = new CallBridgedBookmarkPayload(callControlIdB);
        context.CreateBookmarks(new[]{ bookmarkA, bookmarkB }, ResumeAsync);
    }

    protected abstract ValueTask HandleDisconnectedAsync(ActivityExecutionContext context);
    protected abstract ValueTask HandleBridgedAsync(ActivityExecutionContext context);
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

public record BridgedCallsOutput(CallBridgedPayload PayloadA, CallBridgedPayload PayloadB);