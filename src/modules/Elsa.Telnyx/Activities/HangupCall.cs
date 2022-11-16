using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Refit;

namespace Elsa.Telnyx.Activities
{
    /// <summary>
    /// Hang up the call.
    /// </summary>
    [Activity(Constants.Namespace, "Hang up the call.", Kind = ActivityKind.Task)]
    [FlowNode("Done", "Disconnected")]
    public class HangupCall : ActivityBase
    {
        /// <summary>
        /// Unique identifier and token for controlling the call.
        /// </summary>
        [Input(DisplayName = "Call Control ID", Description = "Unique identifier and token for controlling the call.", Category = "Advanced")]
        public Input<string?>? CallControlId { get; set; } = default!;

        /// <inheritdoc />
        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var callControlId = context.GetCallControlId(CallControlId) ?? throw new Exception("CallControlId is required.");
            var request = new HangupCallRequest();
            var telnyxClient = context.GetRequiredService<ITelnyxClient>();

            try
            {
                await telnyxClient.Calls.HangupCallAsync(callControlId, request, context.CancellationToken);
                await context.CompleteActivityWithOutcomesAsync("Done");
            }
            catch (ApiException e)
            {
                if (!await e.CallIsNoLongerActiveAsync()) throw;
                await context.CompleteActivityWithOutcomesAsync("Disconnected");
            }
        }
    }
}