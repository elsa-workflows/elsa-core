using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Implementations;

public class WorkflowDefinitionActivityMaterializer : IWorkflowDefinitionActivityMaterializer
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly IServiceProvider _serviceProvider;

    public WorkflowDefinitionActivityMaterializer(IWorkflowDefinitionStore store, SerializerOptionsProvider serializerOptionsProvider, IServiceProvider serviceProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
        _serviceProvider = serviceProvider;
    }

    public async Task<IActivity> MaterializeAsync(WorkflowDefinitionActivity activity, CancellationToken cancellationToken = default)
    {
        var definition = await _store.FindPublishedByDefinitionIdAsync(activity.Type, cancellationToken);

        if (definition == null)
            return new Sequence();

        // Construct the root activity stored in the activity definitions.
        var root = JsonSerializer.Deserialize<IActivity>(definition.StringData!, _serializerOptionsProvider.CreateDefaultOptions())!;

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