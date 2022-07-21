using System.Text.Json;
using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Services;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ActivityDefinitions.Implementations;

public class ActivityDefinitionMaterializer : IActivityDefinitionMaterializer
{
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly IServiceProvider _serviceProvider;

    public ActivityDefinitionMaterializer(SerializerOptionsProvider serializerOptionsProvider, IServiceProvider serviceProvider)
    {
        _serializerOptionsProvider = serializerOptionsProvider;
        _serviceProvider = serviceProvider;
    }

    public async Task<IActivity> MaterializeAsync(ActivityDefinition definition, CancellationToken cancellationToken = default)
    {
        // Construct the root activity stored in the activity definitions.
        var root = JsonSerializer.Deserialize<IActivity>(definition.Data!, _serializerOptionsProvider.CreateDefaultOptions())!;

        // Prefix all activity IDS with the ID of the wrapper to prevent naming collisions.
        var prefix = $"{definition.DefinitionId}_";
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