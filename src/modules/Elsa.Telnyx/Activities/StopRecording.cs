using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Stop recording the call.
/// </summary>
[Activity(Constants.Namespace, "Stop recording the call.", Kind = ActivityKind.Task)]
[FlowNode("Recording stopped", "Disconnected")]
public class StopRecording : ActivityBase
{
    /// <summary>
    /// Unique identifier and token for controlling the call.
    /// </summary>
    [Input(
        DisplayName = "Call Control ID",
        Description = "Unique identifier and token for controlling the call.",
        Category = "Advanced"
    )]
    public Input<string?> CallControlId { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var request = new StopRecordingRequest();
        var callControlId = context.GetPrimaryCallControlId(CallControlId) ?? throw new Exception("CallControlId is required");
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.StopRecordingAsync(callControlId, request, context.CancellationToken);
            await context.CompleteActivityWithOutcomesAsync("Recording stopped");
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await context.CompleteActivityWithOutcomesAsync("Disconnected");
        }
    }

    private static string? EmptyToNull(string? value) => value is "" ? null : value;
}