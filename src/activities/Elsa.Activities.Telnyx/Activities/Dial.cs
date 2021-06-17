using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Client.Models;
using Elsa.Activities.Telnyx.Client.Services;
using Elsa.Activities.Telnyx.Exceptions;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Options;
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
        Description = "Dial a number or SIP URI from a given connection.",
        Outcomes = new[] { TelnyxOutcomeNames.Dialing, TelnyxOutcomeNames.CallInitiated, TelnyxOutcomeNames.Answered, TelnyxOutcomeNames.Hangup, OutcomeNames.Done },
        DisplayName = "Dial"
    )]
    public class Dial : Activity
    {
        private readonly ITelnyxClient _telnyxClient;
        private readonly TelnyxOptions _telnyxOptions;

        public Dial(ITelnyxClient telnyxClient, TelnyxOptions telnyxOptions)
        {
            _telnyxClient = telnyxClient;
            _telnyxOptions = telnyxOptions;
        }

        [ActivityInput(
            Label = "Call Control ID", Hint = "The ID of the Call Control App (formerly ID of the connection) to be used when dialing the destination.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string ConnectionId { get; set; } = default!;

        [ActivityInput(Hint = "The DID or SIP URI to dial out and bridge to the given call.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string To { get; set; } = default!;

        [ActivityInput(
            Hint = "The 'from' number to be used as the caller id presented to the destination ('To' number). The number should be in +E164 format. This attribute will default to the 'From' number of the original call if omitted.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? From { get; set; }

        [ActivityInput(
            Hint =
                "The string to be used as the caller id name (SIP From Display Name) presented to the destination ('To' number). The string should have a maximum of 128 characters, containing only letters, numbers, spaces, and -_~!.+ special characters. If omitted, the display name will be the same as the number in the 'From' field.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? FromDisplayName { get; set; }

        [ActivityInput(
            Hint = "Enables Answering Machine Detection.",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] {"disabled", "detect", "detect_beep", "detect_words", "greeting_end"},
            DefaultValue = "disabled",
            SupportedSyntaxes = new[] {SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public string? AnsweringMachineDetection { get; set; } = "disabled";

        [ActivityInput(
            Label = "Answering Machine Detection Configuration",
            Hint = "Optional configuration parameters to modify answering machine detection performance.",
            UIHint = ActivityInputUIHints.Json,
            Category = PropertyCategories.Advanced)]
        public AnsweringMachineConfig? AnsweringMachineDetectionConfig { get; set; }

        [ActivityInput(
            Label = "Command ID",
            Hint = "Use this field to avoid duplicate commands. Telnyx will ignore commands with the same Command ID.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? CommandId { get; set; }

        [ActivityInput(Label = "Custom Headers", Hint = "Custom headers to be added to the SIP INVITE.", Category = PropertyCategories.Advanced, UIHint = ActivityInputUIHints.Json)]
        public IList<Header>? CustomHeaders { get; set; }

        [ActivityInput(Label = "SIP Authentication Username", Hint = "SIP Authentication username used for SIP challenges.", Category = "SIP Authentication", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? SipAuthUsername { get; set; }

        [ActivityInput(Label = "SIP Authentication Password", Hint = "SIP Authentication password used for SIP challenges.", Category = "SIP Authentication", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? SipAuthPassword { get; set; }

        [ActivityInput(Label = "Time Limit", Hint = "Sets the maximum duration of a Call Control Leg in seconds.", Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public int? TimeLimitSecs { get; set; }

        [ActivityInput(
            Label = "Timeout",
            Hint = "The number of seconds that Telnyx will wait for the call to be answered by the destination to which it is being transferred.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public int? TimeoutSecs { get; set; }

        [ActivityInput(
            Label = "Webhook URL",
            Hint = "Use this field to override the URL for which Telnyx will send subsequent webhooks to for this call.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? WebhookUrl { get; set; }

        [ActivityInput(
            Label = "Webhook URL Method",
            Hint = "HTTP request type used for Webhook URL",
            UIHint = ActivityInputUIHints.Dropdown, Options = new[] { "GET", "POST" },
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? WebhookUrlMethod { get; set; }

        [ActivityInput(
            Hint = "A flag indicating whether this activity should complete immediately or suspend the workflow.",
            Category = PropertyCategories.Advanced,
            DefaultValue = true,
            SupportedSyntaxes = new[] {SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid})]
        public bool SuspendWorkflow { get; set; } = true;

        [ActivityOutput] public DialResponse? DialResponse { get; set; }
        [ActivityOutput] public CallPayload? Output { get; set; }
        [ActivityOutput] public CallAnsweredPayload? AnsweredOutput { get; set; }
        [ActivityOutput] public CallHangupPayload? HangupOutput { get; set; }
        [ActivityOutput] public CallInitiatedPayload? InitiatedOutput { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var response = await DialAsync(context);
            DialResponse = response;

            return !SuspendWorkflow 
                ? Done(response) 
                : Combine(Outcome(TelnyxOutcomeNames.Dialing, response), Suspend());
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var payload = context.GetInput<CallPayload>();
            Output = payload;

            return payload switch
            {
                CallAnsweredPayload answeredPayload => AnsweredOutcome(answeredPayload),
                CallHangupPayload hangupPayload => HangupOutcome(hangupPayload),
                CallInitiatedPayload initiatedPayload => Combine(InitiatedOutcome(initiatedPayload), Suspend()),
                _ => throw new ArgumentOutOfRangeException(nameof(payload))
            };
        }

        private IActivityExecutionResult AnsweredOutcome(CallAnsweredPayload payload)
        {
            AnsweredOutput = payload;
            return Outcome(TelnyxOutcomeNames.Answered, payload);
        }
        
        private IActivityExecutionResult HangupOutcome(CallHangupPayload payload)
        {
            HangupOutput = payload;
            return Outcome(TelnyxOutcomeNames.Hangup, payload);
        }
        
        private IActivityExecutionResult InitiatedOutcome(CallInitiatedPayload payload)
        {
            InitiatedOutput = payload;
            return Outcome(TelnyxOutcomeNames.CallInitiated, payload);
        }

        private async Task<DialResponse> DialAsync(ActivityExecutionContext context)
        {
            var connectionId = string.IsNullOrWhiteSpace(ConnectionId) ? _telnyxOptions.CallControlAppId : ConnectionId;

            if (connectionId == null)
                throw new MissingCallControlAppIdException("No Call Control ID specified and no default value configured");

            var fromNumber = context.GetFromNumber(From);

            var clientState = new ClientStatePayload(context.CorrelationId!).ToBase64();

            var request = new DialRequest(
                connectionId,
                To,
                fromNumber,
                FromDisplayName,
                AnsweringMachineDetection,
                AnsweringMachineDetectionConfig,
                clientState,
                CommandId,
                CustomHeaders,
                SipAuthUsername,
                SipAuthPassword,
                TimeLimitSecs,
                TimeoutSecs,
                WebhookUrl,
                WebhookUrlMethod
            );

            try
            {
                var response = await _telnyxClient.Calls.DialAsync(request, context.CancellationToken);
                return response.Data;
            }
            catch (ApiException e)
            {
                throw new WorkflowException(e.Content ?? e.Message, e);
            }
        }
    }

    public static class DialExtensions
    {
        public static ISetupActivity<Dial> WithConnectionId(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.ConnectionId, value);
        public static ISetupActivity<Dial> WithConnectionId(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.ConnectionId, value);
        public static ISetupActivity<Dial> WithConnectionId(this ISetupActivity<Dial> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.ConnectionId, value);
        public static ISetupActivity<Dial> WithConnectionId(this ISetupActivity<Dial> setup, Func<string?> value) => setup.Set(x => x.ConnectionId, value);
        public static ISetupActivity<Dial> WithConnectionId(this ISetupActivity<Dial> setup, string? value) => setup.Set(x => x.ConnectionId, value);

        public static ISetupActivity<Dial> WithTo(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.To, value);
        public static ISetupActivity<Dial> WithTo(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.To, value);
        public static ISetupActivity<Dial> WithTo(this ISetupActivity<Dial> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.To, value);
        public static ISetupActivity<Dial> WithTo(this ISetupActivity<Dial> setup, Func<string?> value) => setup.Set(x => x.To, value);
        public static ISetupActivity<Dial> WithTo(this ISetupActivity<Dial> setup, string? value) => setup.Set(x => x.To, value);

        public static ISetupActivity<Dial> WithFrom(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.From, value);
        public static ISetupActivity<Dial> WithFrom(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.From, value);
        public static ISetupActivity<Dial> WithFrom(this ISetupActivity<Dial> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.From, value);
        public static ISetupActivity<Dial> WithFrom(this ISetupActivity<Dial> setup, Func<string?> value) => setup.Set(x => x.From, value);
        public static ISetupActivity<Dial> WithFrom(this ISetupActivity<Dial> setup, string? value) => setup.Set(x => x.From, value);

        public static ISetupActivity<Dial> WithFromDisplayName(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.FromDisplayName, value);
        public static ISetupActivity<Dial> WithFromDisplayName(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.FromDisplayName, value);
        public static ISetupActivity<Dial> WithFromDisplayName(this ISetupActivity<Dial> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.FromDisplayName, value);
        public static ISetupActivity<Dial> WithFromDisplayName(this ISetupActivity<Dial> setup, Func<string?> value) => setup.Set(x => x.FromDisplayName, value);
        public static ISetupActivity<Dial> WithFromDisplayName(this ISetupActivity<Dial> setup, string? value) => setup.Set(x => x.FromDisplayName, value);

        public static ISetupActivity<Dial> WithAnsweringMachineDetection(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.AnsweringMachineDetection, value);
        public static ISetupActivity<Dial> WithAnsweringMachineDetection(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.AnsweringMachineDetection, value);
        public static ISetupActivity<Dial> WithAnsweringMachineDetection(this ISetupActivity<Dial> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.AnsweringMachineDetection, value);
        public static ISetupActivity<Dial> WithAnsweringMachineDetection(this ISetupActivity<Dial> setup, Func<string?> value) => setup.Set(x => x.AnsweringMachineDetection, value);
        public static ISetupActivity<Dial> WithAnsweringMachineDetection(this ISetupActivity<Dial> setup, string? value) => setup.Set(x => x.AnsweringMachineDetection, value);

        public static ISetupActivity<Dial> WithAnsweringMachineDetectionConfig(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<AnsweringMachineConfig?>> value) =>
            setup.Set(x => x.AnsweringMachineDetectionConfig, value);

        public static ISetupActivity<Dial> WithAnsweringMachineDetectionConfig(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, AnsweringMachineConfig?> value) => setup.Set(x => x.AnsweringMachineDetectionConfig, value);
        public static ISetupActivity<Dial> WithAnsweringMachineDetectionConfig(this ISetupActivity<Dial> setup, Func<ValueTask<AnsweringMachineConfig?>> value) => setup.Set(x => x.AnsweringMachineDetectionConfig, value);
        public static ISetupActivity<Dial> WithAnsweringMachineDetectionConfig(this ISetupActivity<Dial> setup, Func<AnsweringMachineConfig?> value) => setup.Set(x => x.AnsweringMachineDetectionConfig, value);
        public static ISetupActivity<Dial> WithAnsweringMachineDetectionConfig(this ISetupActivity<Dial> setup, AnsweringMachineConfig? value) => setup.Set(x => x.AnsweringMachineDetectionConfig, value);

        public static ISetupActivity<Dial> WithCommandId(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<Dial> WithCommandId(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<Dial> WithCommandId(this ISetupActivity<Dial> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<Dial> WithCommandId(this ISetupActivity<Dial> setup, Func<string?> value) => setup.Set(x => x.CommandId, value);
        public static ISetupActivity<Dial> WithCommandId(this ISetupActivity<Dial> setup, string? value) => setup.Set(x => x.CommandId, value);

        public static ISetupActivity<Dial> WithCustomHeaders(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<IList<Header>?>> value) => setup.Set(x => x.CustomHeaders, value);
        public static ISetupActivity<Dial> WithCustomHeaders(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, IList<Header>?> value) => setup.Set(x => x.CustomHeaders, value);
        public static ISetupActivity<Dial> WithCustomHeaders(this ISetupActivity<Dial> setup, Func<ValueTask<IList<Header>?>> value) => setup.Set(x => x.CustomHeaders, value);
        public static ISetupActivity<Dial> WithCustomHeaders(this ISetupActivity<Dial> setup, Func<IList<Header>?> value) => setup.Set(x => x.CustomHeaders, value);
        public static ISetupActivity<Dial> WithCustomHeaders(this ISetupActivity<Dial> setup, IList<Header>? value) => setup.Set(x => x.CustomHeaders, value);

        public static ISetupActivity<Dial> WithSipAuthUsername(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.SipAuthUsername, value);
        public static ISetupActivity<Dial> WithSipAuthUsername(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.SipAuthUsername, value);
        public static ISetupActivity<Dial> WithSipAuthUsername(this ISetupActivity<Dial> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.SipAuthUsername, value);
        public static ISetupActivity<Dial> WithSipAuthUsername(this ISetupActivity<Dial> setup, Func<string?> value) => setup.Set(x => x.SipAuthUsername, value);
        public static ISetupActivity<Dial> WithSipAuthUsername(this ISetupActivity<Dial> setup, string? value) => setup.Set(x => x.SipAuthUsername, value);

        public static ISetupActivity<Dial> WithSipAuthPassword(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.SipAuthPassword, value);
        public static ISetupActivity<Dial> WithSipAuthPassword(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.SipAuthPassword, value);
        public static ISetupActivity<Dial> WithSipAuthPassword(this ISetupActivity<Dial> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.SipAuthPassword, value);
        public static ISetupActivity<Dial> WithSipAuthPassword(this ISetupActivity<Dial> setup, Func<string?> value) => setup.Set(x => x.SipAuthPassword, value);
        public static ISetupActivity<Dial> WithSipAuthPassword(this ISetupActivity<Dial> setup, string? value) => setup.Set(x => x.SipAuthPassword, value);

        public static ISetupActivity<Dial> WithTimeLimitSecs(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<int?>> value) => setup.Set(x => x.TimeLimitSecs, value);
        public static ISetupActivity<Dial> WithTimeLimitSecs(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, int?> value) => setup.Set(x => x.TimeLimitSecs, value);
        public static ISetupActivity<Dial> WithTimeLimitSecs(this ISetupActivity<Dial> setup, Func<ValueTask<int?>> value) => setup.Set(x => x.TimeLimitSecs, value);
        public static ISetupActivity<Dial> WithTimeLimitSecs(this ISetupActivity<Dial> setup, Func<int?> value) => setup.Set(x => x.TimeLimitSecs, value);
        public static ISetupActivity<Dial> WithTimeLimitSecs(this ISetupActivity<Dial> setup, int? value) => setup.Set(x => x.TimeLimitSecs, value);

        public static ISetupActivity<Dial> WithTimeoutSecs(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<int?>> value) => setup.Set(x => x.TimeoutSecs, value);
        public static ISetupActivity<Dial> WithTimeoutSecs(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, int?> value) => setup.Set(x => x.TimeoutSecs, value);
        public static ISetupActivity<Dial> WithTimeoutSecs(this ISetupActivity<Dial> setup, Func<ValueTask<int?>> value) => setup.Set(x => x.TimeoutSecs, value);
        public static ISetupActivity<Dial> WithTimeoutSecs(this ISetupActivity<Dial> setup, Func<int?> value) => setup.Set(x => x.TimeoutSecs, value);
        public static ISetupActivity<Dial> WithTimeoutSecs(this ISetupActivity<Dial> setup, int? value) => setup.Set(x => x.TimeoutSecs, value);

        public static ISetupActivity<Dial> WithWebhookUrl(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.WebhookUrl, value);
        public static ISetupActivity<Dial> WithWebhookUrl(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.WebhookUrl, value);
        public static ISetupActivity<Dial> WithWebhookUrl(this ISetupActivity<Dial> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.WebhookUrl, value);
        public static ISetupActivity<Dial> WithWebhookUrl(this ISetupActivity<Dial> setup, Func<string?> value) => setup.Set(x => x.WebhookUrl, value);
        public static ISetupActivity<Dial> WithWebhookUrl(this ISetupActivity<Dial> setup, string? value) => setup.Set(x => x.WebhookUrl, value);

        public static ISetupActivity<Dial> WithWebhookUrlMethod(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<string?>> value) => setup.Set(x => x.WebhookUrlMethod, value);
        public static ISetupActivity<Dial> WithWebhookUrlMethod(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, string?> value) => setup.Set(x => x.WebhookUrlMethod, value);
        public static ISetupActivity<Dial> WithWebhookUrlMethod(this ISetupActivity<Dial> setup, Func<ValueTask<string?>> value) => setup.Set(x => x.WebhookUrlMethod, value);
        public static ISetupActivity<Dial> WithWebhookUrlMethod(this ISetupActivity<Dial> setup, Func<string?> value) => setup.Set(x => x.WebhookUrlMethod, value);
        public static ISetupActivity<Dial> WithWebhookUrlMethod(this ISetupActivity<Dial> setup, string? value) => setup.Set(x => x.WebhookUrlMethod, value);
        
        public static ISetupActivity<Dial> WithSuspendWorkflow(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, ValueTask<bool>> value) => setup.Set(x => x.SuspendWorkflow, value);
        public static ISetupActivity<Dial> WithSuspendWorkflow(this ISetupActivity<Dial> setup, Func<ActivityExecutionContext, bool> value) => setup.Set(x => x.SuspendWorkflow, value);
        public static ISetupActivity<Dial> WithSuspendWorkflow(this ISetupActivity<Dial> setup, Func<ValueTask<bool>> value) => setup.Set(x => x.SuspendWorkflow, value);
        public static ISetupActivity<Dial> WithSuspendWorkflow(this ISetupActivity<Dial> setup, Func<bool> value) => setup.Set(x => x.SuspendWorkflow, value);
        public static ISetupActivity<Dial> WithSuspendWorkflow(this ISetupActivity<Dial> setup, bool value) => setup.Set(x => x.SuspendWorkflow, value);
    }
}