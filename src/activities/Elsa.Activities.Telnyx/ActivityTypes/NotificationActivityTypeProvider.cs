using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Activities.Telnyx.Webhooks.Attributes;
using Elsa.Activities.Telnyx.Webhooks.Models;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Activities.Telnyx.Webhooks.Services;
using Elsa.ActivityResults;
using Elsa.Metadata;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Telnyx.ActivityTypes
{
    internal class NotificationActivityTypeProvider : IActivityTypeProvider
    {
        private readonly IWebhookFilterService _webhookFilterService;

        public NotificationActivityTypeProvider(IWebhookFilterService webhookFilterService)
        {
            _webhookFilterService = webhookFilterService;
        }

        public const string NotificationAttribute = "TelnyxNotification";
        public const string EventTypeAttribute = "EventType";

        public ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken = default)
        {
            var activityTypes = GetActivityTypes();
            return new ValueTask<IEnumerable<ActivityType>>(activityTypes);
        }

        private IEnumerable<ActivityType> GetActivityTypes()
        {
            var payloadTypes = GetType().Assembly.GetAllWithBaseClass<Payload>().Where(x => x.GetCustomAttribute<WebhookAttribute>() != null).ToList();
            var activityTypes = payloadTypes.Select(CreateWebhookActivityType).ToList();

            // Add variations on the same webhooks. The webhook filters will conditionally select the appropriate one.
            activityTypes.Add(CreateWebhookActivityTypeVariation<CallInitiatedPayload>("BridgeCallInitiated", "Bridge Call Initiated", "Triggered when an incoming bridging call was received."));
            activityTypes.Add(CreateWebhookActivityTypeVariation<CallHangupPayload>("OriginatorCallHangup", "Originator Call Hangup", "Triggered when an incoming call was hangup by the originator."));

            return activityTypes;
        }

        private ActivityType CreateWebhookActivityTypeVariation<T>(string activityType, string displayName, string description)
        {
            var hangupWebhookAttribute = typeof(T).GetCustomAttribute<WebhookAttribute>()!;
            return CreateWebhookActivityType(new WebhookAttribute(hangupWebhookAttribute.EventType, activityType, displayName, description));
        }

        private static ActivityType CreateWebhookActivityType(Type payloadType)
        {
            var webhookAttribute = payloadType.GetCustomAttribute<WebhookAttribute>();

            if (webhookAttribute == null)
                throw new InvalidOperationException($"Make sure that the payload type is annotated with the ${nameof(WebhookAttribute)} attribute");

            return CreateWebhookActivityType(webhookAttribute);
        }

        private static ActivityType CreateWebhookActivityType(WebhookAttribute webhookAttribute)
        {
            var typeName = webhookAttribute.ActivityType;
            var displayName = webhookAttribute.DisplayName;
            var description = webhookAttribute.Description;

            return new ActivityType
            {
                Describe = () => new ActivityDescriptor
                {
                    Category = "Telnyx",
                    Description = description,
                    Type = typeName,
                    Outcomes = new[] { OutcomeNames.Done },
                    Traits = ActivityTraits.Trigger,
                    DisplayName = displayName
                },
                Description = description,
                DisplayName = displayName,
                TypeName = typeName,
                Attributes = new Dictionary<string, object>
                {
                    [NotificationAttribute] = true,
                    [EventTypeAttribute] = webhookAttribute.EventType
                },
                CanExecuteAsync = _ => new ValueTask<bool>(true),
                ExecuteAsync = context => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal(context) : new ValueTask<IActivityExecutionResult>(new SuspendResult()),
                ResumeAsync = ExecuteInternal,
            };
        }

        private static ValueTask<IActivityExecutionResult> ExecuteInternal(ActivityExecutionContext context)
        {
            var webhook = (TelnyxWebhook) context.Input!;

            if (webhook.Data.Payload is CallPayload callPayload)
            {
                context.WorkflowExecutionContext.CorrelationId ??= callPayload.CallSessionId;

                if (!context.HasCallControlId())
                    context.SetCallControlId(callPayload.CallControlId);

                if (callPayload is CallInitiatedPayload callInitiatedPayload)
                    if (!context.HasFromNumber())
                        context.SetFromNumber(callInitiatedPayload.To);
            }

            return new(new CombinedResult(new OutputResult(context.Input), new DoneResult()));
        }
    }
}