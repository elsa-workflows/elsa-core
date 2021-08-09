using System.Collections.Generic;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Activities.Telnyx.Webhooks.Models;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Telnyx.Activities
{
    [Trigger(
        Description = "Triggered when an inbound phone call is received for any of the specified source or destination phone numbers.",
        Category = Constants.Category)]
    public class FilteredCallInitiated : Activity
    {
        [ActivityInput(
            Hint = "A list of destination numbers to respond to.",
            UIHint = ActivityInputUIHints.MultiText,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public ICollection<string> To { get; set; } = new List<string>();

        [ActivityInput(
            Hint = "A list of source numbers to respond to.",
            UIHint = ActivityInputUIHints.MultiText,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public ICollection<string> From { get; set; } = new List<string>();

        [ActivityOutput] public TelnyxWebhook? Model { get; set; }
        [ActivityOutput] public CallInitiatedPayload? Output { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal(context) : Suspend();
        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternal(context);

        private IActivityExecutionResult ExecuteInternal(ActivityExecutionContext context)
        {
            var webhookModel = (TelnyxWebhook)context.Input!;
            var callInitiatedPayload = (CallInitiatedPayload)webhookModel.Data.Payload;

            context.WorkflowExecutionContext.CorrelationId = callInitiatedPayload.CallSessionId;

            if (!context.HasCallControlId())
                context.SetCallControlId(callInitiatedPayload.CallControlId);

            if (!context.HasFromNumber())
                context.SetFromNumber(callInitiatedPayload.To);

            context.SetCallerNumber(callInitiatedPayload.From);

            Model = webhookModel;
            Output = callInitiatedPayload;
            return Done();
        }
    }
}