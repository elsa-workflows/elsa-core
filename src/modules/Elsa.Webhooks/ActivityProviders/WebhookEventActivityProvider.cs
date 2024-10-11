using Elsa.Webhooks.Activities;
using Elsa.Workflows;
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
        var descriptorTasks = webhookSource.EventTypes
            .Where(x => x.ActivityBinding != null)
            .Select(async eventType => await CreateActivityDescriptorAsync(webhookSource, eventType, cancellationToken))
            .ToList();

        return await Task.WhenAll(descriptorTasks);
    }

    private async Task<ActivityDescriptor> CreateActivityDescriptorAsync(WebhookSource webhookSource, WebhookSourceEventType eventType, CancellationToken cancellationToken = default)
    {
        var activityBinding = eventType.ActivityBinding!;
        var eventTypeDescription = string.IsNullOrWhiteSpace(activityBinding.Description) ? $"Handles {eventType.EventType} webhook events" : activityBinding.Description;
        var fullTypeName = activityBinding.TypeName; //webhookSource.GetWebhookActivityTypeName(eventType.EventType);
        var activityDescriptor = await activityDescriber.DescribeActivityAsync(typeof(WebhookEventReceived), cancellationToken);
        
        activityDescriptor.TypeName = fullTypeName;
        activityDescriptor.Name = activityBinding.DisplayName;
        activityDescriptor.DisplayName = activityBinding.DisplayName;
        activityDescriptor.Category = webhookSource.Name;
        activityDescriptor.Description = eventTypeDescription;
        activityDescriptor.Constructor = context =>
        {
            var activity = (WebhookEventReceived)activityFactory.Create(typeof(WebhookEventReceived), context);
            activity.Type = fullTypeName;
            activity.EventType = eventType.EventType;
            activity.PayloadType = eventType.PayloadType;
            return activity;
        };
        
        var eventTypeDescriptor = activityDescriptor.Inputs.First(x => x.Name == nameof(WebhookEventReceived.EventType));
        var eventPayloadTypeDescriptor = activityDescriptor.Inputs.First(x => x.Name == nameof(WebhookEventReceived.PayloadType));
        var payloadOutputDescriptor = activityDescriptor.Outputs.First(x => x.Name == nameof(WebhookEventReceived.Payload));
        
        eventTypeDescriptor.IsBrowsable = false;
        eventPayloadTypeDescriptor.IsBrowsable = false;
        payloadOutputDescriptor.Type = eventType.PayloadType ?? typeof(object);
        
        return activityDescriptor;
    }
}