using Elsa.ActivityDefinitions.Activities;
using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Extensions;
using Elsa.ActivityDefinitions.Services;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Services;
using Open.Linq.AsyncExtensions;

namespace Elsa.ActivityDefinitions.Implementations;

public class ActivityDefinitionActivityProvider : IActivityProvider
{
    private readonly IActivityDefinitionStore _store;
    private readonly IActivityFactory _activityFactory;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public ActivityDefinitionActivityProvider(IActivityDefinitionStore store, IActivityFactory activityFactory, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _activityFactory = activityFactory;
        _serializerOptionsProvider = serializerOptionsProvider;
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
        var typeName = definition.Name!;

        return new()
        {
            Category = "Custom",
            Description = definition.Description,
            DisplayName = definition.Name,
            ActivityType = typeName,
            Kind = ActivityKind.Action,
            IsBrowsable = true,
            Constructor = context =>
            {
                var activity = (ActivityDefinitionActivity)_activityFactory.Create(typeof(ActivityDefinitionActivity), context);
                activity.TypeName = typeName;
                activity.DefinitionId = definition.DefinitionId;

                return activity;
            }
        };
    }
}