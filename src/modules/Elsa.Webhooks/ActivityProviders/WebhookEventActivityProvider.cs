using Elsa.Webhooks.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Humanizer;
using WebhooksCore;

namespace Elsa.Webhooks.ActivityProviders;

/// <summary>
/// An activity provider that generates activity types based on webhook sources.
/// </summary>
public class WebhookEventActivityProvider(IWebhookSourceProvider webhookSourceProvider, IActivityFactory activityFactory, IActivityDescriber activityDescriber) : IActivityProvider
{
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var sources = await webhookSourceProvider.ListAsync(cancellationToken);
        var activities = (await Task.WhenAll(sources.Select(async x => await CreateActivityDescriptors(x, cancellationToken)))).SelectMany(x => x).ToList();
        return activities;
    }
    
    private async Task<IEnumerable<ActivityDescriptor>> CreateActivityDescriptors(WebhookSource webhookSource, CancellationToken cancellationToken = default)
    {
        var descriptorTasks = webhookSource.EventTypes.Select(async eventType => await CreateActivityDescriptorAsync(webhookSource, eventType, cancellationToken)).ToList();

        return await Task.WhenAll(descriptorTasks);
    }

    private async Task<ActivityDescriptor> CreateActivityDescriptorAsync(WebhookSource webhookSource, WebhookSourceEventType eventType, CancellationToken cancellationToken = default)
    {
        var webhookSourceName = webhookSource.Name.Dehumanize();
        var eventTypeDescription = string.IsNullOrWhiteSpace(eventType.Description) ? $"Handles {eventType.EventType} webhook events" : eventType.Description;
        var ns = $"Webhooks.{webhookSourceName}";
        var fullTypeName = $"{ns}.{eventType.EventType.Dehumanize()}";
        var activityDescriptor = await activityDescriber.DescribeActivityAsync(typeof(WebhookEventReceived), cancellationToken);
        
        activityDescriptor.TypeName = fullTypeName;
        activityDescriptor.Name = eventType.DisplayName;
        activityDescriptor.DisplayName = eventType.DisplayName;
        activityDescriptor.Category = webhookSource.Name;
        activityDescriptor.Description = eventTypeDescription;
        activityDescriptor.Constructor = context =>
        {
            var activity = activityFactory.Create(typeof(WebhookEventReceived), context);
            activity.Type = fullTypeName;
            return activity;
        };
        
        var eventTypeDescriptor = activityDescriptor.Inputs.First(x => x.Name == nameof(WebhookEventReceived.EventType));
        var eventPayloadTypeDescriptor = activityDescriptor.Inputs.First(x => x.Name == nameof(WebhookEventReceived.PayloadType));
        var payloadOutputDescriptor = activityDescriptor.Outputs.First(x => x.Name == nameof(WebhookEventReceived.Payload));
        
        eventTypeDescriptor.IsReadOnly = true;
        eventTypeDescriptor.DefaultValue = eventType.EventType;
        eventTypeDescriptor.ValueGetter = _ => new Input<string>(eventType.EventType);
        eventTypeDescriptor.ValueSetter = (_, _) => { };

        eventPayloadTypeDescriptor.IsBrowsable = false;
        eventPayloadTypeDescriptor.ValueGetter = _ => new Input<Type>(eventType.PayloadType ?? typeof(object));

        payloadOutputDescriptor.Type = eventType.PayloadType ?? typeof(object);
        
        return activityDescriptor;
    }
}