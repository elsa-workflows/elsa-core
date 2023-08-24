using Elsa.Extensions;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Hang up the call.
/// </summary>
[Activity(Constants.Namespace, "Hang up the call.", Kind = ActivityKind.Task)]
public abstract class HangupCallBase : Activity
{
    /// <inheritdoc />
    protected HangupCallBase(string? source = default, int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// Unique identifier and token for controlling the call.
    /// </summary>
    [Input(DisplayName = "Call Control ID", Description = "Unique identifier and token for controlling the call.", Category = "Advanced")]
    public Input<string> CallControlId { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var callControlId = CallControlId.Get(context);
        var request = new HangupCallRequest(ClientState: context.CreateCorrelatingClientState());
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

    /// <summary>
    /// Executed when the call was hangup.
    /// </summary>
    protected abstract ValueTask HandleDoneAsync(ActivityExecutionContext context);
    
    /// <summary>
    /// Executed when the call was no longer active.
    /// </summary>
    protected abstract ValueTask HandleDisconnectedAsync(ActivityExecutionContext context);
}