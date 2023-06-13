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
        Description = "Start recording the call.",
        Outcomes = new[] { TelnyxOutcomeNames.FinishedRecording, TelnyxOutcomeNames.CallIsNoLongerActive },
        DisplayName = "Start Recording"
    )]
    public class StartRecording : Activity
    {
        private readonly ITelnyxClient _telnyxClient;
        public StartRecording(ITelnyxClient telnyxClient) => _telnyxClient = telnyxClient;

        [ActivityInput(
            Label = "Call Control ID",
            Hint = "Unique identifier and token for controlling the call",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? CallControlId { get; set; } = default!;

        [ActivityInput(
            Hint = "When 'dual', final audio file will be stereo recorded with the first leg on channel A, and the rest on channel B.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "single", "dual" },
            DefaultValue = "single",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Channels { get; set; } = "single";

        [ActivityInput(
            Hint = "The audio file format used when storing the call recording. Can be either 'mp3' or 'wav'.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "wav", "mp3" },
            DefaultValue = "wav",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Format { get; set; } = "wav";

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

        [ActivityInput(Hint = "If enabled, a beep sound will be played at the start of a recording.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public bool? PlayBeep { get; set; }

        [ActivityOutput] public CallRecordingSaved? SavedRecordingPayload { get; set; }

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
            SavedRecordingPayload = context.GetInput<CallRecordingSaved>();
            context.LogOutputProperty(this, "Received Payload", SavedRecordingPayload);
            return Outcome(TelnyxOutcomeNames.FinishedRecording);
        }

        private static string? EmptyToNull(string? value) => value is "" ? null : value;
    }

    public static class StartRecordingExtensions
    {
        public static ISetupActivity<StartRecording> WithCallControlId(this ISetupActivity<StartRecording> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<StartRecording> WithCallControlId(this ISetupActivity<StartRecording> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<StartRecording> WithCallControlId(this ISetupActivity<StartRecording> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<StartRecording> WithCallControlId(this ISetupActivity<StartRecording> setup, Func<string?> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<StartRecording> WithCallControlId(this ISetupActivity<StartRecording> setup, string? value) => setup.Set(x => x.CallControlId, value);

        public static ISetupActivity<StartRecording> WithChannels(this ISetupActivity<StartRecording> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.Channels, value);
        public static ISetupActivity<StartRecording> WithChannels(this ISetupActivity<StartRecording> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.Channels, value);
        public static ISetupActivity<StartRecording> WithChannels(this ISetupActivity<StartRecording> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.Channels, value);
        public static ISetupActivity<StartRecording> WithChannels(this ISetupActivity<StartRecording> setup, Func<string?> value) => setup.Set(x => x.Channels, value);
        public static ISetupActivity<StartRecording> WithChannels(this ISetupActivity<StartRecording> setup, string? value) => setup.Set(x => x.Channels, value);

        public static ISetupActivity<StartRecording> WithFormat(this ISetupActivity<StartRecording> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.Format, value);
        public static ISetupActivity<StartRecording> WithFormat(this ISetupActivity<StartRecording> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.Format, value);
        public static ISetupActivity<StartRecording> WithFormat(this ISetupActivity<StartRecording> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.Format, value);
        public static ISetupActivity<StartRecording> WithFormat(this ISetupActivity<StartRecording> setup, Func<string?> value) => setup.Set(x => x.Format, value);
        public static ISetupActivity<StartRecording> WithFormat(this ISetupActivity<StartRecording> setup, string? value) => setup.Set(x => x.Format, value);

        public static ISetupActivity<StartRecording> WithPlayBeep(this ISetupActivity<StartRecording> setup, Func<ActivityExecutionContext, ValueTask<bool?>> value) => setup.Set(x => x.PlayBeep, value);
        public static ISetupActivity<StartRecording> WithPlayBeep(this ISetupActivity<StartRecording> setup, Func<ActivityExecutionContext, bool?> value) => setup.Set(x => x.PlayBeep, value);
        public static ISetupActivity<StartRecording> WithPlayBeep(this ISetupActivity<StartRecording> setup, Func<ValueTask<bool?>> value) => setup.Set(x => x.PlayBeep, value);
        public static ISetupActivity<StartRecording> WithPlayBeep(this ISetupActivity<StartRecording> setup, Func<bool?> value) => setup.Set(x => x.PlayBeep, value);
        public static ISetupActivity<StartRecording> WithPlayBeep(this ISetupActivity<StartRecording> setup, bool? value) => setup.Set(x => x.PlayBeep, value);
    }
}