using Elsa.OrchardCore.Activities;
using Elsa.OrchardCore.Models;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.OrchardCore.ActivityProviders;

/// <summary>
/// An activity provider that generates activity types based on Orchard content type events.
/// </summary>
[UsedImplicitly]
public class OrchardContentItemsEventActivityProvider(IOptions<OrchardCoreOptions> options, IActivityFactory activityFactory, IActivityDescriber activityDescriber) : IActivityProvider
{
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var contentTypes = options.Value.ContentTypes;
        var matrix =
            from contentType in contentTypes
            from eventType in WebhookEventTypes.GetWebhookEventDescriptors()
            select new
            {
                contentType,
                eventType
            };
        var activities = (await Task.WhenAll(matrix.Select(async x => await CreateActivityDescriptorAsync(x.contentType, x.eventType, cancellationToken)))).ToList();
        return activities;
    }

    private async Task<ActivityDescriptor> CreateActivityDescriptorAsync(string contentType, WebhookEventDescriptor eventDescriptor, CancellationToken cancellationToken = default)
    {
        var eventType = eventDescriptor.EventType;
        var humanizedEventName = eventType.Humanize();
        var description = $"Handles {humanizedEventName} events for {contentType} content items";
        var name = $"{contentType}{eventType}";
        var displayName = $"{contentType} {eventType}";
        var fullTypeName = OrchardCoreActivityNameHelper.GetContentItemEventActivityFullTypeName(contentType, eventType);
        var activityType = typeof(ContentItemEvent);
        var activityDescriptor = await activityDescriber.DescribeActivityAsync(activityType, cancellationToken);

        activityDescriptor.TypeName = fullTypeName;
        activityDescriptor.Name = name;
        activityDescriptor.DisplayName = displayName;
        activityDescriptor.Category = "Orchard Core";
        activityDescriptor.Description = description;
        activityDescriptor.Constructor = context =>
        {
            var activity = (ContentItemEvent)activityFactory.Create(activityType, context);
            activity.Type = fullTypeName;
            activity.ContentType = contentType;
            activity.EventType = eventType;
            return activity;
        };

        foreach (var inputDescriptor in activityDescriptor.Inputs)
            inputDescriptor.IsBrowsable = false;

        return activityDescriptor;
    }
}