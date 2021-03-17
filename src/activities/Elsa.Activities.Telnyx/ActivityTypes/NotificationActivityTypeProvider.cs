using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Webhooks.Attributes;
using Elsa.Activities.Telnyx.Webhooks.Models;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.ActivityProviders;
using Elsa.ActivityResults;
using Elsa.Metadata;
using Elsa.Services.Models;

namespace Elsa.Activities.Telnyx.ActivityTypes
{
    public class NotificationActivityTypeProvider : IActivityTypeProvider
    {
        public const string NotificationAttribute = "TelnyxNotification";
        public const string EventTypeAttribute = "EventType";
        
        public ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken = default)
        {
            var activityTypes = GetActivityTypes();
            return new ValueTask<IEnumerable<ActivityType>>(activityTypes);
        }

        private IEnumerable<ActivityType> GetActivityTypes()
        {
            var types = GetType().Assembly.GetAllWithBaseClass<Payload>().Where(x => x.GetCustomAttribute<PayloadAttribute>() != null);
            return types.Select(CreateWebhookActivityType);
        }
        
        private static ActivityType CreateWebhookActivityType(Type payloadType)
        {
            var payloadAttribute = payloadType.GetCustomAttribute<PayloadAttribute>();

            if (payloadAttribute == null)
                throw new InvalidOperationException($"Make sure that the payload type is annotated with the ${nameof(PayloadAttribute)} attribute");

            var typeName = payloadAttribute.ActivityType;
            var displayName = payloadAttribute.DisplayName;
            var description = payloadAttribute.Description;

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
                    [EventTypeAttribute] = payloadAttribute.EventType
                },
                CanExecuteAsync = _ => new ValueTask<bool>(true),
                ExecuteAsync = context => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal(context) : new ValueTask<IActivityExecutionResult>(new SuspendResult()),
                ResumeAsync = ExecuteInternal,
            };
        }
        
        private static ValueTask<IActivityExecutionResult> ExecuteInternal(ActivityExecutionContext context)
        {
            var webhook = (TelnyxWebhook) context.Input!; 
            context.WorkflowExecutionContext.CorrelationId ??= (webhook.Data.Payload as ICorrelationId)?.CorrelationId;
            return new(new CombinedResult(new OutputResult(context.Input), new DoneResult()));
        }
    }
}