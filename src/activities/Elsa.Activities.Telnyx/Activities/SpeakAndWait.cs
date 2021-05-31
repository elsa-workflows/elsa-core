using Elsa.Activities.ControlFlow;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Activities
{
    [Action(
        Category = Constants.Category,
        Description = "Convert text to speech and play it back on the call. When done speaking, control is returned to the workflow.",
        Outcomes = new[] {OutcomeNames.Done, TelnyxOutcomeNames.CallIsNoLongerActive},
        DisplayName = "Speak Text And Wait"
    )]
    public class SpeakAndWait : CompositeActivity
    {
        [ActivityInput(
            Label = "Call Control ID",
            Hint = "Unique identifier and token for controlling the call",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? CallControlId
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "The language you want spoken.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] {"en-US", "en-AU", "nl-NL", "es-ES", "ru-RU"},
            DefaultValue = "en-US",
            SupportedSyntaxes = new[] {SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string Language
        {
            get => GetState<string>(() => "en-US");
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "The gender of the voice used to speak back the text.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] {"female", "male"},
            DefaultValue = "female",
            SupportedSyntaxes = new[] {SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string Voice
        {
            get => GetState<string>(() => "female");
            set => SetState(value);
        }

        [ActivityInput(Hint = "The text or SSML to be converted into speech. There is a 5,000 character limit.", SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public string Payload
        {
            get => GetState<string>()!;
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "The type of the provided payload. The payload can either be plain text, or Speech Synthesis Markup Language (SSML).",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] {"", "text", "ssml"},
            SupportedSyntaxes = new[] {SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? PayloadType
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "This parameter impacts speech quality, language options and payload types. When using `basic`, only the `en-US` language and payload type `text` are allowed.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] {"", "basic", "premium"},
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? ServiceLevel
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityInput(
            Hint = "Use this field to add state to every subsequent webhook. It must be a valid Base-64 encoded string.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? ClientState 
        {
            get => GetState<string>();
            set => SetState(value);
        }

        [ActivityInput(
            Label = "Command ID",
            Hint = "Use this field to avoid duplicate commands. Telnyx will ignore commands with the same Command ID.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? CommandId 
        {
            get => GetState<string>();
            set => SetState(value);
        }

        public override void Build(ICompositeActivityBuilder builder)
        {
            builder
                .StartWith<SpeakText>(speakText => speakText
                        .WithLanguage(() => Language)
                        .WithPayload(() => Payload)
                        .WithPayloadType(() => PayloadType)
                        .WithVoice(() => Voice)
                        .WithClientState(() => ClientState)
                        .WithCommandId(() => CommandId)
                        .WithServiceLevel(() => ServiceLevel)
                        .WithCallControlId(() => CallControlId)
                    , speakText =>
                    {
                        speakText
                            .When(OutcomeNames.Done)
                            .ThenTypeNamed(nameof(CallSpeakEnded))
                            .Finish(OutcomeNames.Done);
                        
                        speakText.When(TelnyxOutcomeNames.CallIsNoLongerActive).Finish(TelnyxOutcomeNames.CallIsNoLongerActive);
                    });
        }
    }
}