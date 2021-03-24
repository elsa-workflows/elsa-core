using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Webhooks.Attributes;
using Elsa.Activities.Telnyx.Webhooks.Models;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Activities.Telnyx.Webhooks.Services;
using Elsa.ActivityProviders;
using Elsa.ActivityResults;
using Elsa.Metadata;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Authorization.Policy;

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
            var hangupWebhookAttribute = payloadTypes.First(x => x == typeof(CallHangupPayload)).GetCustomAttribute<WebhookAttribute>()!;
            activityTypes.Add(CreateWebhookActivityType(new WebhookAttribute(hangupWebhookAttribute.EventType, "OriginatorCallHangup", "Originator Call Hangup", "Triggered when an incoming call was hangup by the originator.")));

            return activityTypes;
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
                context.SetVariable("CallControlId", callPayload.CallControlId);
            }

            return new(new CombinedResult(new OutputResult(context.Input), new DoneResult()));
        }
    }
}