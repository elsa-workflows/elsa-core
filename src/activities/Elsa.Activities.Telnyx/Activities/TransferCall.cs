using System;
using System.Collections.Generic;
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
    [Action(
        Category = Constants.Category,
        Description = "Transfer a call to a new destination",
        Outcomes = new[]
        {
            TelnyxOutcomeNames.Transferring, 
            TelnyxOutcomeNames.CallInitiated, 
            TelnyxOutcomeNames.Bridged, 
            TelnyxOutcomeNames.Answered, 
            TelnyxOutcomeNames.Hangup
        },
        DisplayName = "Transfer Call"
    )]
    public class TransferCall : Activity
    {
        private readonly ITelnyxClient _telnyxClient;

        public TransferCall(ITelnyxClient telnyxClient)
        {
            _telnyxClient = telnyxClient;
        }

        [ActivityInput(
            Label = "Call Control ID",
            Hint = "Unique identifier and token for controlling the call.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? CallControlId { get; set; } = default!;

        [ActivityInput(Label = "To", Hint = "The DID or SIP URI to dial out and bridge to the given call.", SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public string To { get; set; } = default!;

        [ActivityInput(
            Hint = "The 'from' number to be used as the caller id presented to the destination ('To' number). The number should be in +E164 format. This attribute will default to the 'From' number of the original call if omitted.",
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? From { get; set; }

        [ActivityInput(
            Hint =
                "The string to be used as the caller id name (SIP From Display Name) presented to the destination ('To' number). The string should have a maximum of 128 characters, containing only letters, numbers, spaces, and -_~!.+ special characters. If omitted, the display name will be the same as the number in the 'From' field.",
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? FromDisplayName { get; set; }

        [ActivityInput(
            Label = "Answering Machine Detection",
            Hint = "Enables Answering Machine Detection.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] {"disabled", "detect", "detect_beep", "detect_words", "greeting_end"},
            SupportedSyntaxes = new[] {SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? AnsweringMachineDetection { get; set; }

        [ActivityInput(
            Label = "Answering Machine Detection Configuration",
            Hint = "Optional configuration parameters to modify answering machine detection performance.",
            Category = PropertyCategories.Advanced,
            UIHint = ActivityInputUIHints.Json
        )]
        public AnsweringMachineConfig? AnsweringMachineDetectionConfig { get; set; }

        [ActivityInput(
            Label = "Command ID",
            Hint = "Use this field to avoid duplicate commands. Telnyx will ignore commands with the same Command ID.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? CommandId { get; set; }

        [ActivityInput(
            Label = "Audio URL",
            Hint = "Audio URL to be played back when the transfer destination answers before bridging the call. The URL can point to either a WAV or MP3 file.",
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public Uri? AudioUrl { get; set; }

        [ActivityInput(
            Hint = "Use this field to add state to every subsequent webhook. It must be a valid Base-64 encoded string.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public string? ClientState { get; set; }

        [ActivityInput(
            Hint = "Use this field to add state to every subsequent webhook for the new leg. It must be a valid Base-64 encoded string.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? TargetLegClientState { get; set; }

        [ActivityInput(Hint = "Custom headers to be added to the SIP INVITE.", Category = PropertyCategories.Advanced, UIHint = ActivityInputUIHints.Json)]
        public IList<Header>? CustomHeaders { get; set; }

        [ActivityInput(Label = "SIP Authentication Username", Hint = "SIP Authentication username used for SIP challenges.", Category = "SIP Authentication", SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public string? SipAuthUsername { get; set; }

        [ActivityInput(Label = "SIP Authentication Password", Hint = "SIP Authentication password used for SIP challenges.", Category = "SIP Authentication", SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public string? SipAuthPassword { get; set; }

        [ActivityInput(Label = "Time Limit", Hint = "Sets the maximum duration of a Call Control Leg in seconds.", Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public int? TimeLimitSecs { get; set; }

        [ActivityInput(
            Label = "Timeout",
            Hint = "The number of seconds that Telnyx will wait for the call to be answered by the destination to which it is being transferred.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public int? TimeoutSecs { get; set; }

        [ActivityInput(
            Label = "Webhook URL",
            Hint = "Use this field to override the URL for which Telnyx will send subsequent webhooks to for this call.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? WebhookUrl { get; set; }

        [ActivityInput(
            Label = "Webhook URL Method",
            Hint = "HTTP request type used for Webhook URL",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] {"GET", "POST"},
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] {SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid}
        )]
        public string? WebhookUrlMethod { get; set; }
        
        [ActivityOutput] public CallPayload? Output { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await TransferCallAsync(context);
            return Combine(Outcome(TelnyxOutcomeNames.Transferring), Suspend());
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var payload = context.GetInput<CallPayload>();
            Output = payload;

            return payload switch
            {
                CallAnsweredPayload => Outcome(TelnyxOutcomeNames.Answered),
                CallBridgedPayload => Outcome(TelnyxOutcomeNames.Bridged),
                CallHangupPayload => Outcome(TelnyxOutcomeNames.Hangup),
                CallInitiatedPayload => Outcome(TelnyxOutcomeNames.CallInitiated),
                _ => throw new ArgumentOutOfRangeException(nameof(payload))
            };
        }

        private async ValueTask TransferCallAsync(ActivityExecutionContext context)
        {
            var fromNumber = context.GetFromNumber(From);

            var request = new TransferCallRequest(
                To,
                fromNumber,
                FromDisplayName,
                AudioUrl,
                AnsweringMachineDetection,
                AnsweringMachineDetectionConfig,
                ClientState,
                TargetLegClientState,
                CommandId,
                CustomHeaders,
                SipAuthUsername,
                SipAuthPassword,
                TimeLimitSecs,
                TimeoutSecs,
                WebhookUrl,
                WebhookUrlMethod
            );

            var callControlId = context.GetCallControlId(CallControlId);

            try
            {
                await _telnyxClient.Calls.TransferCallAsync(callControlId, request, context.CancellationToken);
            }
            catch (ApiException e)
            {
                throw new WorkflowException(e.Content ?? e.Message, e);
            }
        }
    }

    public static class TransferCallExtensions
    {
        public static ISetupActivity<TransferCall> WithCallControlId(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<TransferCall> WithCallControlId(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<TransferCall> WithCallControlId(this ISetupActivity<TransferCall> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<TransferCall> WithCallControlId(this ISetupActivity<TransferCall> setup, Func<string?> value) => setup.Set(x => x.CallControlId, value);
        public static ISetupActivity<TransferCall> WithCallControlId(this ISetupActivity<TransferCall> setup, string? value) => setup.Set(x => x.CallControlId, value);

        public static ISetupActivity<TransferCall> WithTo(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.To, value);
        public static ISetupActivity<TransferCall> WithTo(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.To, value);
        public static ISetupActivity<TransferCall> WithTo(this ISetupActivity<TransferCall> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.To, value);
        public static ISetupActivity<TransferCall> WithTo(this ISetupActivity<TransferCall> setup, Func<string?> value) => setup.Set(x => x.To, value);
        public static ISetupActivity<TransferCall> WithTo(this ISetupActivity<TransferCall> setup, string? value) => setup.Set(x => x.To, value);

        public static ISetupActivity<TransferCall> WithFrom(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.From, value);
        public static ISetupActivity<TransferCall> WithFrom(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.From, value);
        public static ISetupActivity<TransferCall> WithFrom(this ISetupActivity<TransferCall> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.From, value);
        public static ISetupActivity<TransferCall> WithFrom(this ISetupActivity<TransferCall> setup, Func<string?> value) => setup.Set(x => x.From, value);
        public static ISetupActivity<TransferCall> WithFrom(this ISetupActivity<TransferCall> setup, string? value) => setup.Set(x => x.From, value);

        public static ISetupActivity<TransferCall> WithFromDisplayName(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.FromDisplayName, value);
        public static ISetupActivity<TransferCall> WithFromDisplayName(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.FromDisplayName, value);
        public static ISetupActivity<TransferCall> WithFromDisplayName(this ISetupActivity<TransferCall> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.FromDisplayName, value);
        public static ISetupActivity<TransferCall> WithFromDisplayName(this ISetupActivity<TransferCall> setup, Func<string?> value) => setup.Set(x => x.FromDisplayName, value);
        public static ISetupActivity<TransferCall> WithFromDisplayName(this ISetupActivity<TransferCall> setup, string? value) => setup.Set(x => x.FromDisplayName, value);

        public static ISetupActivity<TransferCall> WithAnsweringMachineDetection(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.AnsweringMachineDetection, value);
        public static ISetupActivity<TransferCall> WithAnsweringMachineDetection(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.AnsweringMachineDetection, value);
        public static ISetupActivity<TransferCall> WithAnsweringMachineDetection(this ISetupActivity<TransferCall> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.AnsweringMachineDetection, value);
        public static ISetupActivity<TransferCall> WithAnsweringMachineDetection(this ISetupActivity<TransferCall> setup, Func<string?> value) => setup.Set(x => x.AnsweringMachineDetection, value);
        public static ISetupActivity<TransferCall> WithAnsweringMachineDetection(this ISetupActivity<TransferCall> setup, string? value) => setup.Set(x => x.AnsweringMachineDetection, value);

        public static ISetupActivity<TransferCall> WithAnsweringMachineDetectionConfig(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<AnsweringMachineConfig?>> value) =>
            setup.Set(x => x.AnsweringMachineDetectionConfig, value);

        public static ISetupActivity<TransferCall> WithAnsweringMachineDetectionConfig(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, AnsweringMachineConfig?> value) =>
            setup.Set(x => x.AnsweringMachineDetectionConfig, value);

        public static ISetupActivity<TransferCall> WithAnsweringMachineDetectionConfig(this ISetupActivity<TransferCall> setup, Func<ValueTask<AnsweringMachineConfig?>> value) => setup.Set(x => x.AnsweringMachineDetectionConfig, value);
        public static ISetupActivity<TransferCall> WithAnsweringMachineDetectionConfig(this ISetupActivity<TransferCall> setup, Func<AnsweringMachineConfig?> value) => setup.Set(x => x.AnsweringMachineDetectionConfig, value);
        public static ISetupActivity<TransferCall> WithAnsweringMachineDetectionConfig(this ISetupActivity<TransferCall> setup, AnsweringMachineConfig? value) => setup.Set(x => x.AnsweringMachineDetectionConfig, value);

        public static ISetupActivity<TransferCall> WithCommandId(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<TransferCall> WithCommandId(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<TransferCall> WithCommandId(this ISetupActivity<TransferCall> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<TransferCall> WithCommandId(this ISetupActivity<TransferCall> setup, Func<string?> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<TransferCall> WithCommandId(this ISetupActivity<TransferCall> setup, string? value) => setup.Set(x => x.CommandId, value);

        public static ISetupActivity<TransferCall> WithClientState(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.ClientState, value);
        public static ISetupActivity<TransferCall> WithClientState(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.ClientState, value);
        public static ISetupActivity<TransferCall> WithClientState(this ISetupActivity<TransferCall> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.ClientState, value);
        public static ISetupActivity<TransferCall> WithClientState(this ISetupActivity<TransferCall> setup, Func<string?> value) => setup.Set(x => x.ClientState, value);
        public static ISetupActivity<TransferCall> WithClientState(this ISetupActivity<TransferCall> setup, string? value) => setup.Set(x => x.ClientState, value);

        public static ISetupActivity<TransferCall> WithCustomHeaders(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<IList<Header>?>> value) => setup.Set(x => x.CustomHeaders, value);
        public static ISetupActivity<TransferCall> WithCustomHeaders(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, IList<Header>?> value) => setup.Set(x => x.CustomHeaders, value);
        public static ISetupActivity<TransferCall> WithCustomHeaders(this ISetupActivity<TransferCall> setup, Func<ValueTask<IList<Header>?>> value) => setup.Set(x => x.CustomHeaders, value);
        public static ISetupActivity<TransferCall> WithCustomHeaders(this ISetupActivity<TransferCall> setup, Func<IList<Header>?> value) => setup.Set(x => x.CustomHeaders, value);
        public static ISetupActivity<TransferCall> WithCustomHeaders(this ISetupActivity<TransferCall> setup, IList<Header>? value) => setup.Set(x => x.CustomHeaders, value);

        public static ISetupActivity<TransferCall> WithSipAuthUsername(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.SipAuthUsername, value);
        public static ISetupActivity<TransferCall> WithSipAuthUsername(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.SipAuthUsername, value);
        public static ISetupActivity<TransferCall> WithSipAuthUsername(this ISetupActivity<TransferCall> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.SipAuthUsername, value);
        public static ISetupActivity<TransferCall> WithSipAuthUsername(this ISetupActivity<TransferCall> setup, Func<string?> value) => setup.Set(x => x.SipAuthUsername, value);
        public static ISetupActivity<TransferCall> WithSipAuthUsername(this ISetupActivity<TransferCall> setup, string? value) => setup.Set(x => x.SipAuthUsername, value);

        public static ISetupActivity<TransferCall> WithSipAuthPassword(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.SipAuthPassword, value);
        public static ISetupActivity<TransferCall> WithSipAuthPassword(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.SipAuthPassword, value);
        public static ISetupActivity<TransferCall> WithSipAuthPassword(this ISetupActivity<TransferCall> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.SipAuthPassword, value);
        public static ISetupActivity<TransferCall> WithSipAuthPassword(this ISetupActivity<TransferCall> setup, Func<string?> value) => setup.Set(x => x.SipAuthPassword, value);
        public static ISetupActivity<TransferCall> WithSipAuthPassword(this ISetupActivity<TransferCall> setup, string? value) => setup.Set(x => x.SipAuthPassword, value);

        public static ISetupActivity<TransferCall> WithTimeLimitSecs(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<int?>> value) => setup.Set(x => x.TimeLimitSecs, value);
        public static ISetupActivity<TransferCall> WithTimeLimitSecs(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, int?> value) => setup.Set(x => x.TimeLimitSecs, value);
        public static ISetupActivity<TransferCall> WithTimeLimitSecs(this ISetupActivity<TransferCall> setup, Func<ValueTask<int?>> value) => setup.Set(x => x.TimeLimitSecs, value);
        public static ISetupActivity<TransferCall> WithTimeLimitSecs(this ISetupActivity<TransferCall> setup, Func<int?> value) => setup.Set(x => x.TimeLimitSecs, value);
        public static ISetupActivity<TransferCall> WithTimeLimitSecs(this ISetupActivity<TransferCall> setup, int? value) => setup.Set(x => x.TimeLimitSecs, value);

        public static ISetupActivity<TransferCall> WithTimeoutSecs(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<int?>> value) => setup.Set(x => x.TimeoutSecs, value);
        public static ISetupActivity<TransferCall> WithTimeoutSecs(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, int?> value) => setup.Set(x => x.TimeoutSecs, value);
        public static ISetupActivity<TransferCall> WithTimeoutSecs(this ISetupActivity<TransferCall> setup, Func<ValueTask<int?>> value) => setup.Set(x => x.TimeoutSecs, value);
        public static ISetupActivity<TransferCall> WithTimeoutSecs(this ISetupActivity<TransferCall> setup, Func<int?> value) => setup.Set(x => x.TimeoutSecs, value);
        public static ISetupActivity<TransferCall> WithTimeoutSecs(this ISetupActivity<TransferCall> setup, int? value) => setup.Set(x => x.TimeoutSecs, value);

        public static ISetupActivity<TransferCall> WithWebhookUrl(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.WebhookUrl, value);
        public static ISetupActivity<TransferCall> WithWebhookUrl(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.WebhookUrl, value);
        public static ISetupActivity<TransferCall> WithWebhookUrl(this ISetupActivity<TransferCall> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.WebhookUrl, value);
        public static ISetupActivity<TransferCall> WithWebhookUrl(this ISetupActivity<TransferCall> setup, Func<string?> value) => setup.Set(x => x.WebhookUrl, value);
        public static ISetupActivity<TransferCall> WithWebhookUrl(this ISetupActivity<TransferCall> setup, string? value) => setup.Set(x => x.WebhookUrl, value);

        public static ISetupActivity<TransferCall> WithWebhookUrlMethod(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.WebhookUrlMethod, value);
        public static ISetupActivity<TransferCall> WithWebhookUrlMethod(this ISetupActivity<TransferCall> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.WebhookUrlMethod, value);
        public static ISetupActivity<TransferCall> WithWebhookUrlMethod(this ISetupActivity<TransferCall> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.WebhookUrlMethod, value);
        public static ISetupActivity<TransferCall> WithWebhookUrlMethod(this ISetupActivity<TransferCall> setup, Func<string?> value) => setup.Set(x => x.WebhookUrlMethod, value);
        public static ISetupActivity<TransferCall> WithWebhookUrlMethod(this ISetupActivity<TransferCall> setup, string? value) => setup.Set(x => x.WebhookUrlMethod, value);
    }
}