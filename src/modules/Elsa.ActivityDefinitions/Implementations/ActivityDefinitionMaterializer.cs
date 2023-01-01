using System.Text.Json;
using Elsa.ActivityDefinitions.Activities;
using Elsa.ActivityDefinitions.Services;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ActivityDefinitions.Implementations;

public class ActivityDefinitionMaterializer : IActivityDefinitionMaterializer
{
    private readonly IActivityDefinitionStore _store;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly IServiceProvider _serviceProvider;

    public ActivityDefinitionMaterializer(IActivityDefinitionStore store, SerializerOptionsProvider serializerOptionsProvider, IServiceProvider serviceProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
        _serviceProvider = serviceProvider;
    }

    public async Task<IActivity> MaterializeAsync(ActivityDefinitionActivity activity, CancellationToken cancellationToken = default)
    {
        var definition = await _store.FindByTypeAsync(activity.Type, activity.Version, cancellationToken);

        if (definition == null)
            return new Sequence();

        // Construct the root activity stored in the activity definitions.
        var root = JsonSerializer.Deserialize<IActivity>(definition.Data!, _serializerOptionsProvider.CreateDefaultOptions())!;

        // Prefix all activity IDS with the ID of the wrapper to prevent naming collisions.
        var prefix = $"{activity.Id}_";
        await ApplyPrefixAsync(prefix, root, cancellationToken);

        return root;
    }

    private async Task ApplyPrefixAsync(string prefix, IActivity root, CancellationToken cancellationToken)
    {
        // Resolve lazily instead of via constructor to avoid a circular dependency.
        var activityWalker = _serviceProvider.GetRequiredService<IActivityWalker>();
        var graph = await activityWalker.WalkAsync(root, cancellationToken);
        var nodes = graph.Flatten().Distinct().ToList();

        foreach (var node in nodes)
            node.Activity.Id = $"{prefix}{node.Activity.Id}";
    }
}