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
using Elsa.Exceptions;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Refit;

namespace Elsa.Activities.Telnyx.Activities
{
    [Job(
        Category = Constants.Category,
        Description = "Convert text to speech and play it back on the call.",
        Outcomes = new[] { TelnyxOutcomeNames.CallIsNoLongerActive, TelnyxOutcomeNames.FinishedSpeaking },
        DisplayName = "Speak Text"
    )]
    public class SpeakText : Activity
    {
        private readonly ITelnyxClient _telnyxClient;
        public SpeakText(ITelnyxClient telnyxClient) => _telnyxClient = telnyxClient;

        [ActivityInput(
            Label = "Call Control ID",
            Hint = "Unique identifier and token for controlling the call",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CallControlId { get; set; } = default!;

        [ActivityInput(
            Hint = "The language you want spoken.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "en-US", "en-AU", "nl-NL", "es-ES", "ru-RU" },
            DefaultValue = "en-US",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Language { get; set; } = "en-US";

        [ActivityInput(
            Hint = "The gender of the voice used to speak back the text.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "female", "male" },
            DefaultValue = "female",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Voice { get; set; } = "female";

        [ActivityInput(Hint = "The text or SSML to be converted into speech. There is a 5,000 character limit.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Payload { get; set; } = default!;

        [ActivityInput(
            Hint = "The type of the provided payload. The payload can either be plain text, or Speech Synthesis Markup Language (SSML).",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "", "text", "ssml" },
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? PayloadType { get; set; }

        [ActivityInput(
            Hint = "This parameter impacts speech quality, language options and payload types. When using `basic`, only the `en-US` language and payload type `text` are allowed.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "", "basic", "premium" },
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? ServiceLevel { get; set; }

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
        
        [ActivityOutput(Hint = "The received payload when speaking ended.")]
        public CallSpeakEnded? SpeakEndedPayload { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var request = new SpeakTextRequest(
                Language,
                Voice,
                Payload,
                EmptyToNull(PayloadType),
                EmptyToNull(ServiceLevel),
                EmptyToNull(ClientState),
                EmptyToNull(CommandId),
                null
            );

            var callControlId = context.GetCallControlId(CallControlId);

            try
            {
                await _telnyxClient.Calls.SpeakTextAsync(callControlId, request, context.CancellationToken);
                return Suspend();
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
            SpeakEndedPayload = context.GetInput<CallSpeakEnded>();
            context.LogOutputProperty(this, "Received Payload", SpeakEndedPayload);
            return Outcome(TelnyxOutcomeNames.FinishedSpeaking);
        }

        private static string? EmptyToNull(string? value) => value is "" ? null : value;
    }

    public static class SpeakTextExtensions
    {
        public static ISetupActivity<SpeakText> WithCallControlId(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<SpeakText> WithCallControlId(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<SpeakText> WithCallControlId(this ISetupActivity<SpeakText> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<SpeakText> WithCallControlId(this ISetupActivity<SpeakText> setup, Func<string?> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<SpeakText> WithCallControlId(this ISetupActivity<SpeakText> setup, string? value) => setup.Set(x => x.CallControlId, value);
        
        public static ISetupActivity<SpeakText> WithLanguage(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, ValueTask<string>> value) => setup.Set(x => x.Language, value!);
        public static ISetupActivity<SpeakText> WithLanguage(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, string> value) => setup.Set(x => x.Language, value);
        public static ISetupActivity<SpeakText> WithLanguage(this ISetupActivity<SpeakText> setup, Func<ValueTask<string>> value) => setup.Set(x => x.Language, value!);
        public static ISetupActivity<SpeakText> WithLanguage(this ISetupActivity<SpeakText> setup, Func<string> value) => setup.Set(x => x.Language, value);
        public static ISetupActivity<SpeakText> WithLanguage(this ISetupActivity<SpeakText> setup, string value) => setup.Set(x => x.Language, value);
        
        public static ISetupActivity<SpeakText> WithVoice(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, ValueTask<string>> value) => setup.Set(x => x.Voice, value!);
        public static ISetupActivity<SpeakText> WithVoice(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, string> value) => setup.Set(x => x.Voice, value);
        public static ISetupActivity<SpeakText> WithVoice(this ISetupActivity<SpeakText> setup, Func<ValueTask<string>> value) => setup.Set(x => x.Voice, value!);
        public static ISetupActivity<SpeakText> WithVoice(this ISetupActivity<SpeakText> setup, Func<string> value) => setup.Set(x => x.Voice, value);
        public static ISetupActivity<SpeakText> WithVoice(this ISetupActivity<SpeakText> setup, string value) => setup.Set(x => x.Voice, value);
        
        public static ISetupActivity<SpeakText> WithPayload(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, ValueTask<string>> value) => setup.Set(x => x.Payload, value!);
        public static ISetupActivity<SpeakText> WithPayload(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, string> value) => setup.Set(x => x.Payload, value);
        public static ISetupActivity<SpeakText> WithPayload(this ISetupActivity<SpeakText> setup, Func<ValueTask<string>> value) => setup.Set(x => x.Payload, value!);
        public static ISetupActivity<SpeakText> WithPayload(this ISetupActivity<SpeakText> setup, Func<string> value) => setup.Set(x => x.Payload, value);
        public static ISetupActivity<SpeakText> WithPayload(this ISetupActivity<SpeakText> setup, string value) => setup.Set(x => x.Payload, value);
        
        public static ISetupActivity<SpeakText> WithPayloadType(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.PayloadType, value);
        public static ISetupActivity<SpeakText> WithPayloadType(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.PayloadType, value);
        public static ISetupActivity<SpeakText> WithPayloadType(this ISetupActivity<SpeakText> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.PayloadType, value);
        public static ISetupActivity<SpeakText> WithPayloadType(this ISetupActivity<SpeakText> setup, Func<string?> value) => setup.Set(x => x.PayloadType, value);
        public static ISetupActivity<SpeakText> WithPayloadType(this ISetupActivity<SpeakText> setup, string? value) => setup.Set(x => x.PayloadType, value);
        
        public static ISetupActivity<SpeakText> WithServiceLevel(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.ServiceLevel, value);
        public static ISetupActivity<SpeakText> WithServiceLevel(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.ServiceLevel, value);
        public static ISetupActivity<SpeakText> WithServiceLevel(this ISetupActivity<SpeakText> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.ServiceLevel, value);
        public static ISetupActivity<SpeakText> WithServiceLevel(this ISetupActivity<SpeakText> setup, Func<string?> value) => setup.Set(x => x.ServiceLevel, value);
        public static ISetupActivity<SpeakText> WithServiceLevel(this ISetupActivity<SpeakText> setup, string? value) => setup.Set(x => x.ServiceLevel, value);
        
        public static ISetupActivity<SpeakText> WithClientState(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.ClientState, value);
        public static ISetupActivity<SpeakText> WithClientState(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.ClientState, value);
        public static ISetupActivity<SpeakText> WithClientState(this ISetupActivity<SpeakText> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.ClientState, value);
        public static ISetupActivity<SpeakText> WithClientState(this ISetupActivity<SpeakText> setup, Func<string?> value) => setup.Set(x => x.ClientState, value);
        public static ISetupActivity<SpeakText> WithClientState(this ISetupActivity<SpeakText> setup, string? value) => setup.Set(x => x.ClientState, value);
        
        public static ISetupActivity<SpeakText> WithCommandId(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<SpeakText> WithCommandId(this ISetupActivity<SpeakText> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<SpeakText> WithCommandId(this ISetupActivity<SpeakText> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<SpeakText> WithCommandId(this ISetupActivity<SpeakText> setup, Func<string?> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<SpeakText> WithCommandId(this ISetupActivity<SpeakText> setup, string? value) => setup.Set(x => x.CommandId, value);
    }
}