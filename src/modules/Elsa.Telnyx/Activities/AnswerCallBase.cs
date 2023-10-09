using Elsa.Extensions;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Answer an incoming call. You must issue this command before executing subsequent commands on an incoming call.
/// </summary>
[Activity(Constants.Namespace, "Answer an incoming call. You must issue this command before executing subsequent commands on an incoming call.", Kind = ActivityKind.Task)]
public abstract class AnswerCallBase : Activity<CallAnsweredPayload>
{
    /// <inheritdoc />
    protected AnswerCallBase(string? source = default, int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The call control ID to answer. Leave blank when the workflow is driven by an incoming call and you wish to pick up that one.
    /// </summary>
    [Input(DisplayName = "Call Control ID", Description = "The call control ID of the call to answer.", Category = "Advanced")]
    public Input<string> CallControlId { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var callControlId = CallControlId.Get(context);

        var request = new AnswerCallRequest
        {
            ClientState = context.CreateCorrelatingClientState(context.Id)
        };

        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            // Send a request to Telnyx to answer the call.
            await telnyxClient.Calls.AnswerCallAsync(callControlId, request, context.CancellationToken);

            // Create a bookmark so we can resume the workflow when the call is answered.
            context.CreateBookmark(new AnswerCallBookmarkPayload(callControlId), ResumeAsync, true);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await HandleDisconnectedAsync(context);
        }
    }

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
        var payload = context.GetWorkflowInput<CallAnsweredPayload>();
        context.Set(Result, payload);
        await HandleConnectedAsync(context);
    }
}