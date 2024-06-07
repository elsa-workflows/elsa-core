using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Humanizer;

namespace Elsa.Workflows.Services;

/// <inheritdoc />
public class IdentityGraphService : IIdentityGraphService
{
    private readonly IActivityVisitor _activityVisitor;
    private readonly IActivityRegistryLookupService _activityRegistryLookup;

    /// <summary>
    /// Constructor.
    /// </summary>
    public IdentityGraphService(IActivityVisitor activityVisitor, IActivityRegistryLookupService activityRegistryLookup)
    {
        _activityVisitor = activityVisitor;
        _activityRegistryLookup = activityRegistryLookup;
    }

    /// <inheritdoc />
    public async Task AssignIdentitiesAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        await AssignIdentitiesAsync((IActivity)workflow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AssignIdentitiesAsync(IActivity root, CancellationToken cancellationToken = default)
    {
        var graph = await _activityVisitor.VisitAsync(root, cancellationToken);
        await AssignIdentitiesAsync(graph);
    }

    /// <inheritdoc />
    public Task AssignIdentitiesAsync(ActivityNode root) => AssignIdentitiesAsync(root.Flatten().ToList());

    /// <inheritdoc />
    public async Task AssignIdentitiesAsync(ICollection<ActivityNode> flattenedList)
    {
        var identityCounters = new Dictionary<string, int>();

        foreach (var node in flattenedList)
        {
            node.Activity.Id = CreateId(node, identityCounters, flattenedList);
            node.Activity.NodeId = node.NodeId;
            await AssignInputOutputsAsync(node.Activity);

            if (node.Activity is IVariableContainer variableContainer)
                AssignVariables(variableContainer);
        }
    }

    /// <inheritdoc />
    public async Task AssignInputOutputsAsync(IActivity activity)
    {
        var activityDescriptor = await _activityRegistryLookup.FindAsync(activity.Type, activity.Version) ?? throw new Exception("Activity descriptor not found");
        var inputDictionary = activityDescriptor.GetWrappedInputProperties(activity); 

        foreach (var (inputName, input) in inputDictionary)
        {
            var blockReference = input?.MemoryBlockReference();

            if (blockReference == null!) 
                continue;
            
            if (string.IsNullOrEmpty(blockReference.Id))
                blockReference.Id = $"{activity.Id}:input-{inputName.Humanize().Kebaberize()}";
        }
        
        var outputs = activity.GetOutputs();

        foreach (var output in outputs)
        {
            var blockReference = output.Value.MemoryBlockReference();

            if (blockReference == null!) 
                continue;
            
            if (string.IsNullOrEmpty(blockReference.Id))
                blockReference.Id = $"{activity.Id}:output-{output.Name.Humanize().Kebaberize()}";
        }
    }

    /// <inheritdoc />
    public void AssignVariables(IVariableContainer activity)
    {
        var variables = activity.Variables;
        var seed = 0;

        foreach (var variable in variables)
            variable.Id = variable.Id != null! ? variable.Id : $"{activity.Id}:variable-{++seed}";
    }

    private string CreateId(ActivityNode activityNode, IDictionary<string, int> identityCounters, ICollection<ActivityNode> allNodes)
    {
        if (!string.IsNullOrWhiteSpace(activityNode.Activity.Id))
            return activityNode.Activity.Id;

        while (true)
        {
            var fullTypeName = activityNode.Activity.Type;
            var shortTypeName = fullTypeName.Split('.').Last();
            var index = GetNextIndexFor(shortTypeName, identityCounters);
            var name = $"{shortTypeName}{index + 1}";

            if (allNodes.All(x => x.Activity.Id != name))
                return name;
        }
    }

    private int GetNextIndexFor(string activityType, IDictionary<string, int> identityCounters)
    {
        if (!identityCounters.TryGetValue(activityType, out var index))
        {
            identityCounters[activityType] = index;
        }
        else
        {
            index = identityCounters[activityType] + 1;
            identityCounters[activityType] = index;
        }

        return index;
    }
}