using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Elsa.Activities.Telnyx.Client.Services;
using Elsa.Activities.Telnyx.Extensions;
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
    [Action(
        Category = Constants.Category,
        Description = "Start recording the call.",
        Outcomes = new[] { OutcomeNames.Done, TelnyxOutcomeNames.CallIsNoLongerActive },
        DisplayName = "Start Recording"
    )]
    public class StartRecording : Activity
    {
        private readonly ITelnyxClient _telnyxClient;
        public StartRecording(ITelnyxClient telnyxClient) => _telnyxClient = telnyxClient;

        [ActivityProperty(
            Label = "Call Control ID",
            Hint = "Unique identifier and token for controlling the call",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CallControlId { get; set; } = default!;

        [ActivityProperty(
            Hint = "When 'dual', final audio file will be stereo recorded with the first leg on channel A, and the rest on channel B.",
            UIHint = ActivityPropertyUIHints.Dropdown,
            Options = new[] { "single", "dual" },
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Channels { get; set; } = default!;

        [ActivityProperty(
            Hint = "The audio file format used when storing the call recording. Can be either 'mp3' or 'wav'.",
            UIHint = ActivityPropertyUIHints.Dropdown,
            Options = new[] { "wav", "mp3" },
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Format { get; set; } = default!;

        [ActivityProperty(
            Hint = "Use this field to add state to every subsequent webhook. It must be a valid Base-64 encoded string.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? ClientState { get; set; }

        [ActivityProperty(
            Label = "Command ID",
            Hint = "Use this field to avoid duplicate commands. Telnyx will ignore commands with the same Command ID.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CommandId { get; set; }

        [ActivityProperty(Hint = "If enabled, a beep sound will be played at the start of a recording.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public bool? PlayBeep { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var request = new StartRecordingRequest(
                Channels,
                Format,
                EmptyToNull(ClientState),
                EmptyToNull(CommandId),
                PlayBeep
            );

            var callControlId = context.GetCallControlId(CallControlId);

            try
            {
                await _telnyxClient.Calls.StartRecordingAsync(callControlId, request, context.CancellationToken);
                return Done();
            }
            catch (ApiException e)
            {
                if (await e.CallIsNoLongerActiveAsync())
                    return Outcome(TelnyxOutcomeNames.CallIsNoLongerActive);

                throw new WorkflowException(e.Content ?? e.Message, e);
            }
        }

        private static string? EmptyToNull(string? value) => value is "" ? null : value;
    }
}