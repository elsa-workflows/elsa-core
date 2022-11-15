using Elsa.ActivityDefinitions.Activities;
using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Extensions;
using Elsa.ActivityDefinitions.Services;
using Elsa.Common.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Services;
using Open.Linq.AsyncExtensions;

namespace Elsa.ActivityDefinitions.Implementations;

/// <summary>
/// Provides activity descriptors based on <see cref="ActivityDefinition"/>s stored in the database. 
/// </summary>
public class ActivityDefinitionActivityProvider : IActivityProvider
{
    private readonly IActivityDefinitionStore _store;
    private readonly IActivityFactory _activityFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ActivityDefinitionActivityProvider(IActivityDefinitionStore store, IActivityFactory activityFactory)
    {
        _store = store;
        _activityFactory = activityFactory;
    }

    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var definitions = await _store.ListAsync(VersionOptions.All, cancellationToken).ToList();
        var descriptors = CreateDescriptors(definitions).ToList();
        return descriptors;
    }

    private IEnumerable<ActivityDescriptor> CreateDescriptors(IEnumerable<ActivityDefinition> definitions) => definitions.Select(CreateDescriptor);

    private ActivityDescriptor CreateDescriptor(ActivityDefinition definition)
    {
        return new()
        {
            Type = definition.Type,
            Version = definition.Version,
            DisplayName = definition.DisplayName.WithDefault(definition.Type),
            Description = definition.Description,
            Category = definition.Category.WithDefault("Custom"),
            Kind = ActivityKind.Action,
            IsBrowsable = definition.IsPublished,
            Constructor = context =>
            {
                var activity = (ActivityDefinitionActivity)_activityFactory.Create(typeof(ActivityDefinitionActivity), context);
                activity.Type = definition.Type;
                activity.Version = definition.Version;
                return activity;
            }
        };
    }
}