using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Refit;

namespace Elsa.Telnyx.Activities
{
    /// <summary>
    /// Bridge two calls.
    /// </summary>
    [Activity(Constants.Namespace, "Bridge two calls.", Kind = ActivityKind.Task)]
    [FlowNode("Bridged", "Disconnected")]
    public class BridgeCalls : ActivityBase<BridgedCallsOutput>
    {
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
            var callControlIdA = context.GetCallControlId(CallControlIdA) ?? throw new Exception("CallControlA is required");
            var callControlIdB = context.GetCallControlId(CallControlIdA) ?? throw new Exception("CallControlB is required");
            var request = new BridgeCallsRequest(callControlIdB);
            var telnyxClient = context.GetRequiredService<ITelnyxClient>();

            try
            {
                await telnyxClient.Calls.BridgeCallsAsync(callControlIdA, request, context.CancellationToken);
                context.CreateBookmark(ResumeAsync);
            }
            catch (ApiException e)
            {
                if (!await e.CallIsNoLongerActiveAsync()) throw;

                await context.CompleteActivityAsync("Disconnected");
            }
        }

        private async ValueTask ResumeAsync(ActivityExecutionContext context)
        {
            var payload = context.GetInput<CallBridgedPayload>()!;
            var callControlIdA = context.GetCallControlId(CallControlIdA);
            var callControlIdB = context.GetCallControlId(CallControlIdA);

            if (payload.CallControlId == callControlIdA) context.SetProperty("CallBridgedPayloadA", payload);
            if (payload.CallControlId == callControlIdB) context.SetProperty("CallBridgedPayloadB", payload);

            var callBridgedPayloadA = context.GetProperty<CallBridgedPayload>("CallBridgedPayloadA");
            var callBridgedPayloadB = context.GetProperty<CallBridgedPayload>("CallBridgedPayloadB");
            
            if (callBridgedPayloadA != null && callBridgedPayloadB != null)
            {
                context.Set(Result, new BridgedCallsOutput(callBridgedPayloadA, callBridgedPayloadB));
                await context.CompleteActivityAsync(new Outcomes("Bridged"));
                return;
            }

            context.CreateBookmark();
        }
    }

    public record BridgedCallsOutput(CallBridgedPayload PayloadA, CallBridgedPayload PayloadB);
}