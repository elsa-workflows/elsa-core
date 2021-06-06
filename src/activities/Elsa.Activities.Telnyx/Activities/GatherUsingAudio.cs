using System;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Elsa.Activities.Telnyx.Client.Services;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Exceptions;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Refit;

namespace Elsa.Activities.Telnyx.Activities
{
    [Job(
        Category = Constants.Category,
        Description = "Play an audio file on the call until the required DTMF signals are gathered to build interactive menus.",
        Outcomes = new[] { TelnyxOutcomeNames.GatheringInput, TelnyxOutcomeNames.GatherCompleted, TelnyxOutcomeNames.CallIsNoLongerActive },
        DisplayName = "Gather Using Audio"
    )]
    public class GatherUsingAudio : Activity
    {
        private readonly ITelnyxClient _telnyxClient;

        public GatherUsingAudio(ITelnyxClient telnyxClient) => _telnyxClient = telnyxClient;

        [ActivityInput(
            Label = "Call Control ID", Hint = "Unique identifier and token for controlling the call",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CallControlId { get; set; } = default!;

        [ActivityInput(
            Label = "Audio URL",
            Hint = "The URL of a file to be played back at the beginning of each prompt. The URL can point to either a WAV or MP3 file.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public Uri AudioUrl { get; set; } = default!;

        [ActivityInput(Hint = "Use this field to add state to every subsequent webhook. It must be a valid Base-64 encoded string.", Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? ClientState { get; set; }

        [ActivityInput(
            Label = "Command ID",
            Hint = "Use this field to avoid duplicate commands. Telnyx will ignore commands with the same Command ID.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CommandId { get; set; }

        [ActivityInput(
            Label = "Inter Digit Timeout",
            Hint = "The number of milliseconds to wait for input between digits.",
            Category = PropertyCategories.Advanced,
            DefaultValue = 5000,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public int? InterDigitTimeoutMillis { get; set; } = 5000;

        [ActivityInput(
            Label = "Invalid Audio Url",
            Hint = "The URL of a file to play when digits don't match the Valid Digits setting or the number of digits is not between Min and Max. The URL can point to either a WAV or MP3 file.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public Uri? InvalidAudioUrl { get; set; }

        [ActivityInput(
            Hint = "A list of all digits accepted as valid.",
            Category = PropertyCategories.Advanced,
            DefaultValue = "0123456789#*",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? ValidDigits { get; set; } = "0123456789#*";

        [ActivityInput(Hint = "The minimum number of digits to fetch. This parameter has a minimum value of 1.", DefaultValue = 1, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public int? MinimumDigits { get; set; } = 1;

        [ActivityInput(Hint = "The maximum number of digits to fetch. This parameter has a maximum value of 128.", DefaultValue = 128, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public int? MaximumDigits { get; set; } = 128;

        [ActivityInput(Hint = "The maximum number of times the file should be played if there is no input from the user on the call.", DefaultValue = 3, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public int? MaximumTries { get; set; } = 3;

        [ActivityInput(Hint = "The digit used to terminate input if fewer than `maximum_digits` digits have been gathered.", DefaultValue = "#", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? TerminatingDigit { get; set; } = "#";

        [ActivityInput(
            Label = "Timeout",
            Hint = "The number of milliseconds to wait for a DTMF response after file playback ends before a replaying the sound file.",
            Category = PropertyCategories.Advanced,
            DefaultValue = 60000,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public int? TimeoutMillis { get; set; } = 60000;
        
        [ActivityOutput(Hint = "The received payload when gathering completed.")]
        public CallGatherEndedPayload? ReceivedPayload { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var request = new GatherUsingAudioRequest(
                AudioUrl,
                EmptyToNull(ClientState),
                EmptyToNull(CommandId),
                InterDigitTimeoutMillis,
                InvalidAudioUrl,
                MaximumDigits,
                MaximumTries,
                MinimumDigits,
                EmptyToNull(TerminatingDigit),
                TimeoutMillis,
                EmptyToNull(ValidDigits)
            );

            var callControlId = context.GetCallControlId(CallControlId);

            try
            {
                await _telnyxClient.Calls.GatherUsingAudioAsync(callControlId, request, context.CancellationToken);
                return Combine(Outcome(TelnyxOutcomeNames.GatheringInput), Suspend());
            }
            catch (ApiException e)
            {
                if (await e.CallIsNoLongerActiveAsync())
                    return Outcome(TelnyxOutcomeNames.CallIsNoLongerActive);

                throw new WorkflowException(e.Content ?? e.Message, e);
            }
        }
        
        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            ReceivedPayload = context.GetInput<CallGatherEndedPayload>();
            return Outcome(TelnyxOutcomeNames.GatherCompleted);
        }

        private string? EmptyToNull(string? value) => value is "" ? null : value;
    }
}