using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Refit;

namespace Elsa.Telnyx.Activities;

[FlowNode("Done", "Disconnected")]
public class FlowHangupCall : HangupCallBase
{
    protected override ValueTask HandleDoneAsync(ActivityExecutionContext context) => context.CompleteActivityWithOutcomesAsync("Done");
    protected override ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => context.CompleteActivityWithOutcomesAsync("Disconnected");
}

public class HangupCall : HangupCallBase
{
    [Port] public IActivity? Done { get; set; }
    [Port] public IActivity? Disconnected { get; set; }

    protected override async ValueTask HandleDoneAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Done, OnCompletedAsync);
    protected override async ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Disconnected, OnCompletedAsync);
}

/// <summary>
/// Hang up the call.
/// </summary>
[Activity(Constants.Namespace, "Hang up the call.", Kind = ActivityKind.Task)]
public abstract class HangupCallBase : ActivityBase
{
    /// <summary>
    /// Unique identifier and token for controlling the call.
    /// </summary>
    [Input(DisplayName = "Call Control ID", Description = "Unique identifier and token for controlling the call.", Category = "Advanced")]
    public Input<string?>? CallControlId { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var callControlId = context.GetPrimaryCallControlId(CallControlId) ?? throw new Exception("CallControlId is required.");
        var request = new HangupCallRequest();
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.HangupCallAsync(callControlId, request, context.CancellationToken);
            await HandleDoneAsync(context);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await HandleDisconnectedAsync(context);
        }
    }

    protected abstract ValueTask HandleDoneAsync(ActivityExecutionContext context);
    protected abstract ValueTask HandleDisconnectedAsync(ActivityExecutionContext context);
    protected async ValueTask OnCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => await context.CompleteActivityAsync();
}