using Elsa.OrchardCore.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.OrchardCore.ActivityProviders;

/// An activity provider that generates activity types based on Orchard content type events.
[UsedImplicitly]
public class OrchardContentItemsEventActivityProvider(IOptions<OrchardCoreOptions> options, IActivityFactory activityFactory, IActivityDescriber activityDescriber) : IActivityProvider
{
    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var contentTypes = options.Value.ContentTypes;
        var activities = (await Task.WhenAll(contentTypes.Select(async x => await CreateActivityDescriptorAsync(x, cancellationToken)))).ToList();
        return activities;
    }
    
    private async Task<ActivityDescriptor> CreateActivityDescriptorAsync(string contentType, CancellationToken cancellationToken = default)
    {
        var description = $"Handles published events for {contentType} content items";
        var name = $"{contentType}Published";
        var displayName = $"{contentType} Published";
        var fullTypeName = $"Orchard.ContentItem.{contentType}.Published";
        var activityDescriptor = await activityDescriber.DescribeActivityAsync(typeof(ContentItemPublished), cancellationToken);
        
        activityDescriptor.TypeName = fullTypeName;
        activityDescriptor.Name = name;
        activityDescriptor.DisplayName = displayName;
        activityDescriptor.Category = "Orchard";
        activityDescriptor.Description = description;
        activityDescriptor.Constructor = context =>
        {
            var activity = (ContentItemPublished)activityFactory.Create(typeof(ContentItemPublished), context);
            activity.Type = fullTypeName;
            activity.ContentType = contentType;
            return activity;
        };
        
        var contentTypeDescriptor = activityDescriptor.Inputs.First(x => x.Name == nameof(ContentItemPublished.ContentType));
        contentTypeDescriptor.IsBrowsable = false;
        
        return activityDescriptor;
    }
}