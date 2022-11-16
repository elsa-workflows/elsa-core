using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Models;
using Refit;

namespace Elsa.Telnyx.Activities
{
    /// <summary>
    /// Start recording the call.
    /// </summary>
    [Activity(Constants.Namespace, "Start recording the call.", Kind = ActivityKind.Task)]
    [FlowNode("Recording finished", "Disconnected")]
    public class StartRecording : ActivityBase<CallRecordingSaved>
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
        /// When 'dual', final audio file will be stereo recorded with the first leg on channel A, and the rest on channel B.
        /// </summary>
        [Input(
            Description = "When 'dual', final audio file will be stereo recorded with the first leg on channel A, and the rest on channel B.",
            UIHint = InputUIHints.Dropdown,
            Options = new[] { "single", "dual" },
            DefaultValue = "single"
        )]
        public Input<string> Channels { get; set; } = new("single");

        /// <summary>
        /// The audio file format used when storing the call recording. Can be either 'mp3' or 'wav'.
        /// </summary>
        [Input(
            Description = "The audio file format used when storing the call recording. Can be either 'mp3' or 'wav'.",
            UIHint = InputUIHints.Dropdown,
            Options = new[] { "wav", "mp3" },
            DefaultValue = "wav"
        )]
        public Input<string> Format { get; set; } = new("wav");

        /// <summary>
        /// If enabled, a beep sound will be played at the start of a recording.
        /// </summary>
        [Input(Description = "If enabled, a beep sound will be played at the start of a recording.")]
        public Input<bool?>? PlayBeep { get; set; }

        /// <inheritdoc />
        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var request = new StartRecordingRequest(
                Channels.Get(context) ?? "single",
                Format.Get(context) ?? "wav",
                PlayBeep.Get(context)
            );

            var callControlId = context.GetCallControlId(CallControlId) ?? throw new Exception("CallControlId is required.");
            var telnyxClient = context.GetRequiredService<ITelnyxClient>();

            try
            {
                await telnyxClient.Calls.StartRecordingAsync(callControlId, request, context.CancellationToken);
                context.CreateBookmark(ResumeAsync);
            }
            catch (ApiException e)
            {
                if (!await e.CallIsNoLongerActiveAsync()) throw;
                await context.CompleteActivityWithOutcomesAsync("Disconnected");
            }
        }

        private async ValueTask ResumeAsync(ActivityExecutionContext context)
        {
            var payload = context.GetInput<CallRecordingSaved>();
            context.Set(Result, payload);
            await context.CompleteActivityWithOutcomesAsync("Recording finished");
        }
    }
}