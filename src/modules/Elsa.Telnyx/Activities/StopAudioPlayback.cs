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
    /// Stop audio playback.
    /// </summary>
    [Activity(Constants.Namespace, Description = "Stop audio playback.", Kind = ActivityKind.Task)]
    [FlowNode("Playback ended", "Disconnected")]
    public class StopAudioPlayback : ActivityBase
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
        
        /// <summary>
        /// Use 'current' to stop only the current audio or 'all' to stop all audios in the queue.
        /// </summary>
        [Input(
            Description = "Use 'current' to stop only the current audio or 'all' to stop all audios in the queue.",
            DefaultValue = "all",
            Category = "Advanced"
        )]
        public Input<string?> Stop { get; set; } = new("all");
        
        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var request = new StopAudioPlaybackRequest(Stop.Get(context));
            var callControlId = context.GetCallControlId(CallControlId) ?? throw new Exception("CallControlId is required.");
            var telnyxClient = context.GetRequiredService<ITelnyxClient>();

            try
            {
                await telnyxClient.Calls.StopAudioPlaybackAsync(callControlId, request, context.CancellationToken);
                context.CreateBookmark(ResumeAsync);
            }
            catch (ApiException e)
            {
                if (!await e.CallIsNoLongerActiveAsync()) throw;
                await context.CompleteActivityWithOutcomesAsync("Disconnected");
            }
        }

        private async ValueTask ResumeAsync(ActivityExecutionContext context) => await context.CompleteActivityWithOutcomesAsync("Playback ended");
    }
}