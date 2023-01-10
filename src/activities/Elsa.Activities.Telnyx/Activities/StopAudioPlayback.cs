using System;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Elsa.Activities.Telnyx.Client.Services;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Refit;

namespace Elsa.Activities.Telnyx.Activities
{
    [Action(
        Category = Constants.Category,
        Description = "Stop audio playback.",
        Outcomes = new[] { TelnyxOutcomeNames.CallIsNoLongerActive, TelnyxOutcomeNames.CallPlaybackEnded, OutcomeNames.Done },
        DisplayName = "Stop Audio Playback"
    )]
    public class StopAudioPlayback : Activity
    {
        private readonly ITelnyxClient _telnyxClient;
        public StopAudioPlayback(ITelnyxClient telnyxClient) => _telnyxClient = telnyxClient;

        [ActivityInput(
            Label = "Call Control ID",
            Hint = "Unique identifier and token for controlling the call",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CallControlId { get; set; } = default!;

        [ActivityInput(
            Hint = "Use this field to add state to every subsequent webhook. It must be a valid Base-64 encoded string.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? ClientState { get; set; }

        [ActivityInput(
            Label = "Command ID",
            Hint = "Use this field to avoid duplicate commands. Telnyx will ignore commands with the same Command ID.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CommandId { get; set; }

        [ActivityInput(
            Hint = "Use 'current' to stop only the current audio or 'all' to stop all audios in the queue.",
            DefaultValue = "all",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Stop { get; set; } = "all";
        
        [ActivityOutput(Hint = "The received payload when audio ended.")]
        public CallPlaybackEndedPayload? PlaybackEndedPayload { get; set; }
        
        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var request = new StopAudioPlaybackRequest(
                EmptyToNull(ClientState),
                EmptyToNull(CommandId),
                Stop
            );

            var callControlId = context.GetCallControlId(CallControlId);
            
            context.JournalData["CallControlId"] = callControlId;

            try
            {
                await _telnyxClient.Calls.StopAudioPlaybackAsync(callControlId, request, context.CancellationToken);
                return Suspend();
            }
            catch (ApiException e)
            {
                if (await e.CallIsNoLongerActiveAsync())
                    return Outcome(TelnyxOutcomeNames.CallIsNoLongerActive);

                return Done();
            }
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            PlaybackEndedPayload = context.GetInput<CallPlaybackEndedPayload>();
            context.LogOutputProperty(this, "Received Payload", PlaybackEndedPayload);
            return Outcomes(TelnyxOutcomeNames.CallPlaybackEnded, OutcomeNames.Done);
        }

        private static string? EmptyToNull(string? value) => value is "" ? null : value;
    }
    
    public static class StopAudioPlaybackExtensions
    {
        public static ISetupActivity<StopAudioPlayback> WithCallControlId(this ISetupActivity<StopAudioPlayback> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<StopAudioPlayback> WithCallControlId(this ISetupActivity<StopAudioPlayback> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<StopAudioPlayback> WithCallControlId(this ISetupActivity<StopAudioPlayback> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<StopAudioPlayback> WithCallControlId(this ISetupActivity<StopAudioPlayback> setup, Func<string?> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<StopAudioPlayback> WithCallControlId(this ISetupActivity<StopAudioPlayback> setup, string? value) => setup.Set(x => x.CallControlId, value);

        public static ISetupActivity<StopAudioPlayback> WithStop(this ISetupActivity<StopAudioPlayback> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.Stop, value);
        public static ISetupActivity<StopAudioPlayback> WithStop(this ISetupActivity<StopAudioPlayback> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.Stop, value);
        public static ISetupActivity<StopAudioPlayback> WithStop(this ISetupActivity<StopAudioPlayback> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.Stop, value);
        public static ISetupActivity<StopAudioPlayback> WithStop(this ISetupActivity<StopAudioPlayback> setup, Func<string?> value) => setup.Set(x => x.Stop, value);
        public static ISetupActivity<StopAudioPlayback> WithStop(this ISetupActivity<StopAudioPlayback> setup, string? value) => setup.Set(x => x.Stop, value);
        
    }
}