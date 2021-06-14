using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Webhooks.Attributes;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Metadata;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Telnyx.Providers.ActivityTypes
{
    internal class NotificationActivityTypeProvider : IActivityTypeProvider
    {
        private readonly IDescribesActivityType _describesActivityType;
        private readonly IActivityActivator _activityActivator;

        public NotificationActivityTypeProvider(IDescribesActivityType describesActivityType, IActivityActivator activityActivator)
        {
            _describesActivityType = describesActivityType;
            _activityActivator = activityActivator;
        }

        public const string NotificationAttribute = "TelnyxNotification";
        public const string EventTypeAttribute = "EventType";

        public async ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken = default)
        {
            var payloadTypes = GetType().Assembly.GetAllWithBaseClass<Payload>().Where(x => x.GetCustomAttribute<WebhookAttribute>() != null).ToList();
            var activityTypes = (await Task.WhenAll(payloadTypes.Select(async x => await CreateWebhookActivityTypeAsync(x, cancellationToken)))).ToList();

            // Add variations on the same webhooks. The webhook filters will conditionally select the appropriate one.
            activityTypes.Add(await CreateWebhookActivityTypeVariationAsync<CallInitiatedPayload>("BridgeCallInitiated", "Bridge Call Initiated", "Triggered when an incoming bridging call was received.", cancellationToken));
            activityTypes.Add(await CreateWebhookActivityTypeVariationAsync<CallHangupPayload>("OriginatorCallHangup", "Originator Call Hangup", "Triggered when an incoming call was hangup by the originator.", cancellationToken));

            return activityTypes;
        }

        private async Task<ActivityType> CreateWebhookActivityTypeVariationAsync<T>(string activityType, string displayName, string description, CancellationToken cancellationToken)
        {
            var hangupWebhookAttribute = typeof(T).GetCustomAttribute<WebhookAttribute>()!;
            return await CreateWebhookActivityTypeAsync(typeof(T), new WebhookAttribute(hangupWebhookAttribute.EventType, activityType, displayName, description), cancellationToken);
        }

        private async Task<ActivityType> CreateWebhookActivityTypeAsync(Type payloadType, CancellationToken cancellationToken)
        {
            var webhookAttribute = payloadType.GetCustomAttribute<WebhookAttribute>();

            if (webhookAttribute == null)
                throw new InvalidOperationException($"Make sure that the payload type is annotated with the ${nameof(WebhookAttribute)} attribute");

            return await CreateWebhookActivityTypeAsync(payloadType, webhookAttribute, cancellationToken);
        }

        private async Task<ActivityType> CreateWebhookActivityTypeAsync(Type payloadType, WebhookAttribute webhookAttribute, CancellationToken cancellationToken)
        {
            var descriptor = await _describesActivityType.DescribeAsync<Webhook>(cancellationToken);

            descriptor.Description = webhookAttribute.Description;
            descriptor.DisplayName = webhookAttribute.DisplayName;
            descriptor.Type = webhookAttribute.ActivityType;

            var outputProperties = descriptor.OutputProperties.Where(x => x.Name != nameof(Webhook.Output)).ToList();
            outputProperties.Add(new ActivityOutputDescriptor(nameof(Webhook.Output), payloadType));
            descriptor.OutputProperties = outputProperties.ToArray();

            return new ActivityType
            {
                Describe = () => descriptor,
                Description = descriptor.Description,
                DisplayName = descriptor.DisplayName,
                TypeName = descriptor.Type,
                Attributes = new Dictionary<string, object>
                {
                    [NotificationAttribute] = true,
                    [EventTypeAttribute] = webhookAttribute.EventType
                },
                ActivateAsync = async context => await _activityActivator.ActivateActivityAsync<Webhook>(context, cancellationToken),
                CanExecuteAsync = (context, instance) => instance.CanExecuteAsync(context),
                ExecuteAsync = (context, instance) => instance.ExecuteAsync(context),
                ResumeAsync = (context, instance) => instance.ResumeAsync(context),
            };
        }
    }
}