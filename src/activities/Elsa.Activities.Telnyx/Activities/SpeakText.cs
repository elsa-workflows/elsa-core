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
        Description = "Convert text to speech and play it back on the call.",
        Outcomes = new[] { OutcomeNames.Done, TelnyxOutcomeNames.CallIsNoLongerActive },
        DisplayName = "Speak Text"
    )]
    public class SpeakText : Activity
    {
        private readonly ITelnyxClient _telnyxClient;
        public SpeakText(ITelnyxClient telnyxClient) => _telnyxClient = telnyxClient;

        [ActivityProperty(
            Label = "Call Control ID",
            Hint = "Unique identifier and token for controlling the call",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CallControlId { get; set; } = default!;

        [ActivityProperty(
            Hint = "The language you want spoken.",
            UIHint = ActivityPropertyUIHints.Dropdown,
            Options = new[] { "en-US", "en-AU", "nl-NL", "es-ES", "ru-RU" },
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Language { get; set; } = default!;

        [ActivityProperty(
            Hint = "The gender of the voice used to speak back the text.",
            UIHint = ActivityPropertyUIHints.Dropdown,
            Options = new[] { "female", "male" },
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Voice { get; set; } = default!;

        [ActivityProperty(Hint = "The text or SSML to be converted into speech. There is a 5,000 character limit.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Payload { get; set; } = default!;

        [ActivityProperty(
            Hint = "The type of the provided payload. The payload can either be plain text, or Speech Synthesis Markup Language (SSML).",
            UIHint = ActivityPropertyUIHints.Dropdown,
            Options = new[] { "", "text", "ssml" },
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? PayloadType { get; set; }

        [ActivityProperty(
            Hint = "This parameter impacts speech quality, language options and payload types. When using `basic`, only the `en-US` language and payload type `text` are allowed.",
            UIHint = ActivityPropertyUIHints.Dropdown,
            Options = new[] { "", "basic", "premium" },
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? ServiceLevel { get; set; }

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

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var request = new SpeakTextRequest(
                Language,
                Voice,
                Payload,
                EmptyToNull(PayloadType),
                EmptyToNull(ClientState),
                EmptyToNull(CommandId),
                EmptyToNull(ServiceLevel),
                null
            );

            var callControlId = context.GetCallControlId(CallControlId);

            try
            {
                await _telnyxClient.Calls.SpeakTextAsync(callControlId, request, context.CancellationToken);
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