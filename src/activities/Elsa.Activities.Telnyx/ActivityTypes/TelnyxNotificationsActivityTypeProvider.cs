using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Attributes;
using Elsa.Activities.Telnyx.Payloads;
using Elsa.Activities.Telnyx.Payloads.Abstract;
using Elsa.ActivityProviders;
using Elsa.ActivityResults;
using Elsa.Metadata;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Elsa.Activities.Telnyx.ActivityTypes
{
    public class TelnyxNotificationsActivityTypeProvider : IActivityTypeProvider
    {
        private readonly IActivityDescriber _activityDescriber;
        private readonly IActivityActivator _activityActivator;

        public TelnyxNotificationsActivityTypeProvider(IActivityDescriber activityDescriber, IActivityActivator activityActivator)
        {
            _activityDescriber = activityDescriber;
            _activityActivator = activityActivator;
        }

        public ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken = default)
        {
            var activityTypes = GetActivityTypes();
            return new ValueTask<IEnumerable<ActivityType>>(activityTypes);
        }

        private IEnumerable<ActivityType> GetActivityTypes()
        {
            yield return CreateWebhookActivityType<CallInitiatedPayload>();
            yield return CreateWebhookActivityType<CallHangupPayload>();
        }

        private ActivityType CreateWebhookActivityType<TPayload>()
        {
            var activityType = typeof(TelnyxNotification);
            var payloadAttribute = typeof(TPayload).GetCustomAttribute<PayloadAttribute>();

            if (payloadAttribute == null)
                throw new InvalidOperationException($"Make sure that the payload type is annotated with the ${nameof(PayloadAttribute)} attribute");

            var typeName = payloadAttribute.ActivityType;
            var displayName = payloadAttribute.DisplayName;
            var description = payloadAttribute.Description;

            return new ActivityType
            {
                Describe = () =>
                {
                    var descriptor = _activityDescriber.Describe(typeof(TelnyxNotification))!;
                    descriptor.Description = description;
                    descriptor.Type = typeName;
                    descriptor.DisplayName = displayName;
                    return descriptor;
                },
                Type = activityType,
                Description = description,
                DisplayName = displayName,
                TypeName = typeName,
                Attributes = new Dictionary<string, object> { [nameof(TelnyxNotification.EventType)] = payloadAttribute.EventType },
                CanExecuteAsync = ActivateAndExecuteActivity<bool, CallInitiatedPayload>((activity, context) => activity.CanExecuteAsync(context), payloadAttribute),
                ExecuteAsync = ActivateAndExecuteActivity<IActivityExecutionResult, CallInitiatedPayload>((activity, context) => activity.ExecuteAsync(context), payloadAttribute),
                ResumeAsync = ActivateAndExecuteActivity<IActivityExecutionResult, CallInitiatedPayload>((activity, context) => activity.ResumeAsync(context), payloadAttribute),
            };
        }

        private Func<ActivityExecutionContext, ValueTask<TReturn>> ActivateAndExecuteActivity<TReturn, TPayload>(
            Func<TelnyxNotification, ActivityExecutionContext, ValueTask<TReturn>> execute,
            PayloadAttribute payloadAttribute)
            where TPayload : Payload
        {
            async ValueTask<TReturn> Activate(ActivityExecutionContext context)
            {
                var activity = (TelnyxNotification) await _activityActivator.ActivateActivityAsync(context, typeof(TelnyxNotification));
                activity.EventType = payloadAttribute.EventType;
                return await execute(activity, context);
            }

            return Activate;
        }
    }
}