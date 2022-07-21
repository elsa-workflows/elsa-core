using Elsa.ActivityDefinitions.Activities;
using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Extensions;
using Elsa.ActivityDefinitions.Services;
using Elsa.Persistence.Common.Models;
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

    public ActivityDefinitionActivityProvider(IActivityDefinitionStore store, IActivityFactory activityFactory)
    {
        _store = store;
        _activityFactory = activityFactory;
    }

    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        var definitions = await _store.ListAsync(VersionOptions.Published, cancellationToken).ToList();
        var descriptors = CreateDescriptors(definitions).ToList();
        return descriptors;
    }

    private IEnumerable<ActivityDescriptor> CreateDescriptors(IEnumerable<ActivityDefinition> definitions) => definitions.Select(CreateDescriptor);

    private ActivityDescriptor CreateDescriptor(ActivityDefinition definition)
    {
        var typeName = definition.TypeName!;

        return new()
        {
            ActivityType = typeName,
            DisplayName = definition.DisplayName.WithDefault(typeName),
            Description = definition.Description,
            Category = definition.Category.WithDefault("Custom"),
            Kind = ActivityKind.Action,
            IsBrowsable = true,
            Constructor = context =>
            {
                var activity = (ActivityDefinitionActivity)_activityFactory.Create(typeof(ActivityDefinitionActivity), context);
                activity.TypeName = typeName;

                if (string.IsNullOrWhiteSpace(activity.DefinitionId))
                    activity.DefinitionId = definition.DefinitionId;

                if (activity.DefinitionVersion == 0)
                    activity.DefinitionVersion = definition.Version;

                return activity;
            }
        };
    }
}