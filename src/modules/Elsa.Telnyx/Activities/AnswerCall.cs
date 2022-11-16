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
    /// Answer an incoming call. You must issue this command before executing subsequent commands on an incoming call.
    /// </summary>
    [Activity(Constants.Namespace, "Answer an incoming call. You must issue this command before executing subsequent commands on an incoming call.", Kind = ActivityKind.Task)]
    [FlowNode("Connected", "Disconnected")]
    public class AnswerCall : ActivityBase<CallAnsweredPayload>
    {
        /// <summary>
        /// The call control ID to answer. Leave blank when the workflow is driven by an incoming call and you wish to pick up that one.
        /// </summary>
        public Input<string?>? CallControlId { get; set; }

        /// <inheritdoc />
        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var telnyxClient = context.GetRequiredService<ITelnyxClient>();
            
            try
            {
                var callControlId = context.GetCallControlId(CallControlId) ?? throw new Exception("CallControlId is required.");
                var request = new AnswerCallRequest();
                await telnyxClient.Calls.AnswerCallAsync(callControlId, request, context.CancellationToken);

                context.CreateBookmark(ResumeAsync);
            }
            catch (ApiException e)
            {
                if (!await e.CallIsNoLongerActiveAsync()) throw;
                await context.CompleteActivityAsync(new Outcomes("Disconnected"));
            }
        }

        private async ValueTask ResumeAsync(ActivityExecutionContext context)
        {
            var payload = context.GetInput<CallAnsweredPayload>();
            context.Set(Result, payload);
            await context.CompleteActivityAsync(new Outcomes("Connected"));
        }
    }
}