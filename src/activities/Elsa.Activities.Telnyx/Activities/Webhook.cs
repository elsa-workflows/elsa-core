using System.ComponentModel;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Activities.Telnyx.Webhooks.Models;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Telnyx.Activities
{
    [Browsable(false)]
    [Trigger(Category = "Telnyx", Outcomes = new[]{ OutcomeNames.Done })]
    public class Webhook : Activity
    {
        [ActivityOutput] public TelnyxWebhook? Model { get; set; }
        [ActivityOutput] public Payload? Output { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal(context) : Suspend();
        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternal(context);

        private IActivityExecutionResult ExecuteInternal(ActivityExecutionContext context)
        {
            var webhookModel = (TelnyxWebhook) context.Input!;

            if (webhookModel.Data.Payload is CallPayload callPayload)
            {
                if (!context.HasCallControlId())
                    context.SetCallControlId(callPayload.CallControlId);
                
                if(!context.HasCallLegId())
                    context.SetCallLegId(callPayload.CallLegId);

                if (callPayload is CallInitiatedPayload callInitiatedPayload)
                {
                    if (!context.HasFromNumber())
                        context.SetFromNumber(callInitiatedPayload.To);

                    context.SetCallerNumber(callInitiatedPayload.From);
                }
            }

            Model = webhookModel;
            Output = webhookModel.Data.Payload;
            
            context.LogOutputProperty(this, "Webhook Payload", webhookModel);
            return Done();
        }
    }
}