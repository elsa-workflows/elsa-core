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
        Category = "Telnyx",
        Description = "Start real-time transcription. Transcription will stop on call hang-up, or can be initiated via the Transcription stop command.",
        Outcomes = new[] { OutcomeNames.Done, TelnyxOutcomeNames.CallIsNoLongerActive },
        DisplayName = "Start Transcription"
    )]
    public class StartTranscription : Activity
    {
        private readonly ICallsApi _callsApi;
        public StartTranscription(ICallsApi callsApi) => _callsApi = callsApi;

        [ActivityInput(
            Label = "Call Control ID",
            Hint = "Unique identifier and token for controlling the call",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CallControlId { get; set; }

        [ActivityInput(
            Hint = "Indicates which leg of the call will be transcribed. Use inbound for the leg that requested the transcription, outbound for the other leg, and both for both legs of the call. Will default to inbound.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "inbound", "outbound", "both" },
            DefaultValue = "inbound",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string TranscriptionTracks { get; set; } = "inbound";
        
        [ActivityInput(
            Hint = "Language to use for speech recognition.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "en", "de", "es", "fr", "it", "pl" },
            DefaultValue = "en",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Language { get; set; } = "en";

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

        [ActivityInput(Hint = "Whether to send also interim results. If set to false, only final results will be sent.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public bool? InterimResults { get; set; }

        [ActivityOutput] public CallRecordingSaved? SavedRecordingPayload { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var request = new StartTranscriptionRequest(
                TranscriptionTracks,
                Language,
                EmptyToNull(ClientState),
                EmptyToNull(CommandId),
                InterimResults
            );

            var callControlId = context.GetCallControlId(CallControlId);

            try
            {
                await _callsApi.StartTranscriptionAsync(callControlId, request, context.CancellationToken);
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

    public static class StartTranscriptionExtensions
    {
        public static ISetupActivity<StartTranscription> WithCallControlId(this ISetupActivity<StartTranscription> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<StartTranscription> WithCallControlId(this ISetupActivity<StartTranscription> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<StartTranscription> WithCallControlId(this ISetupActivity<StartTranscription> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<StartTranscription> WithCallControlId(this ISetupActivity<StartTranscription> setup, Func<string?> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<StartTranscription> WithCallControlId(this ISetupActivity<StartTranscription> setup, string? value) => setup.Set(x => x.CallControlId, value);
        
        public static ISetupActivity<StartTranscription> WithTranscriptionTracks(this ISetupActivity<StartTranscription> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.TranscriptionTracks, value);
        public static ISetupActivity<StartTranscription> WithTranscriptionTracks(this ISetupActivity<StartTranscription> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.TranscriptionTracks, value);
        public static ISetupActivity<StartTranscription> WithTranscriptionTracks(this ISetupActivity<StartTranscription> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.TranscriptionTracks, value);
        public static ISetupActivity<StartTranscription> WithTranscriptionTracks(this ISetupActivity<StartTranscription> setup, Func<string?> value) => setup.Set(x => x.TranscriptionTracks, value);
        public static ISetupActivity<StartTranscription> WithTranscriptionTracks(this ISetupActivity<StartTranscription> setup, string? value) => setup.Set(x => x.TranscriptionTracks, value);
        
        public static ISetupActivity<StartTranscription> WithLanguage(this ISetupActivity<StartTranscription> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.Language, value);
        public static ISetupActivity<StartTranscription> WithLanguage(this ISetupActivity<StartTranscription> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.Language, value);
        public static ISetupActivity<StartTranscription> WithLanguage(this ISetupActivity<StartTranscription> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.Language, value);
        public static ISetupActivity<StartTranscription> WithLanguage(this ISetupActivity<StartTranscription> setup, Func<string?> value) => setup.Set(x => x.Language, value);
        public static ISetupActivity<StartTranscription> WithLanguage(this ISetupActivity<StartTranscription> setup, string? value) => setup.Set(x => x.Language, value);
        
        public static ISetupActivity<StartTranscription> WithInterimResults(this ISetupActivity<StartTranscription> setup, Func<ActivityExecutionContext, ValueTask<bool?>> value) => setup.Set(x => x.InterimResults, value);
        public static ISetupActivity<StartTranscription> WithInterimResults(this ISetupActivity<StartTranscription> setup, Func<ActivityExecutionContext, bool?> value) => setup.Set(x => x.InterimResults, value);
        public static ISetupActivity<StartTranscription> WithInterimResults(this ISetupActivity<StartTranscription> setup, Func<ValueTask<bool?>> value) => setup.Set(x => x.InterimResults, value);
        public static ISetupActivity<StartTranscription> WithInterimResults(this ISetupActivity<StartTranscription> setup, Func<bool?> value) => setup.Set(x => x.InterimResults, value);
        public static ISetupActivity<StartTranscription> WithInterimResults(this ISetupActivity<StartTranscription> setup, bool? value) => setup.Set(x => x.InterimResults, value);
    }
}