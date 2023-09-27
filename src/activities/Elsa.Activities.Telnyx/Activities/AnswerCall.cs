using System.Threading;
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
using Microsoft.Extensions.Logging;
using Refit;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Telnyx.Activities
{
    [Job(
        Category = Constants.Category,
        Description = "Answer an incoming call. You must issue this command before executing subsequent commands on an incoming call",
        Outcomes = new[] { TelnyxOutcomeNames.Answered, TelnyxOutcomeNames.CallIsNoLongerActive },
        DisplayName = "Answer Call"
    )]
    public class AnswerCall : Activity
    {
        private readonly ITelnyxClient _telnyxClient;
        private readonly ILogger<AnswerCall> _logger;

        public AnswerCall(ITelnyxClient telnyxClient, ILogger<AnswerCall> logger)
        {
            _telnyxClient = telnyxClient;
            _logger = logger;
        }

        [ActivityInput(Label = "Call Control ID", Hint = "Unique identifier and token for controlling the call", Category = PropertyCategories.Advanced, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string CallControlId { get; set; } = default!;

        [ActivityInput(
            Label = "Billing Group ID",
            Hint = "Use this field to set the Billing Group ID for the call. Must be a valid and existing Billing Group ID.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? BillingGroupId { get; set; }

        [ActivityInput(
            Hint = "Use this field to add state to every subsequent webhook. It must be a valid Base-64 encoded string.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? ClientState { get; set; }

        [ActivityInput(
            Label = "Command ID",
            Hint = "Use this field to avoid duplicate commands. Telnyx will ignore commands with the same Command ID.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? CommandId { get; set; }

        [ActivityInput(
            Label = "Webhook URL",
            Hint = "Use this field to override the URL for which Telnyx will send subsequent webhooks to for this call.",
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? WebhookUrl { get; set; }

        [ActivityInput(
            Label = "Webhook URL Method",
            Hint = "HTTP request type used for Webhook URL",
            UIHint = ActivityInputUIHints.Dropdown,
            Options = new[] { "GET", "POST" },
            Category = PropertyCategories.Advanced,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? WebhookUrlMethod { get; set; }

        [ActivityOutput(Hint = "The received payload when the call got answered.")]
        public CallAnsweredPayload? ReceivedPayload { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                var callControlId = context.GetCallControlId(CallControlId);
                context.WorkflowExecutionContext.RegisterTask(async (_, ct) => await AnswerCallAsync(callControlId, ct));
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
            ReceivedPayload = context.GetInput<CallAnsweredPayload>();
            context.LogOutputProperty(this, "Received Payload", ReceivedPayload);
            return Outcome(TelnyxOutcomeNames.Answered);
        }
        
        private async ValueTask AnswerCallAsync(string callControlId, CancellationToken cancellationToken)
        {
            var request = new AnswerCallRequest(BillingGroupId, ClientState, CommandId, WebhookUrl, WebhookUrlMethod);
            await _telnyxClient.Calls.AnswerCallAsync(callControlId, request, cancellationToken);
        }
    }
}