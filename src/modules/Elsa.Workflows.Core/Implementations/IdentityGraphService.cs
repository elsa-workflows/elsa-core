using Elsa.Extensions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

/// <inheritdoc />
public class IdentityGraphService : IIdentityGraphService
{
    private readonly IActivityWalker _activityWalker;

    /// <summary>
    /// Constructor.
    /// </summary>
    public IdentityGraphService(IActivityWalker activityWalker)
    {
        _activityWalker = activityWalker;
    }

    /// <inheritdoc />
    public async Task AssignIdentitiesAsync(Workflow workflow, CancellationToken cancellationToken = default) => await AssignIdentitiesAsync((IActivity)workflow, cancellationToken);

    /// <inheritdoc />
    public async Task AssignIdentitiesAsync(IActivity root, CancellationToken cancellationToken = default)
    {
        var graph = await _activityWalker.WalkAsync(root, cancellationToken);
        AssignIdentities(graph);
    }

    /// <inheritdoc />
    public void AssignIdentities(ActivityNode root) => AssignIdentities(root.Flatten().ToList());

    /// <inheritdoc />
    public void AssignIdentities(ICollection<ActivityNode> flattenedList)
    {
        var identityCounters = new Dictionary<string, int>();

        foreach (var node in flattenedList)
        {
            node.Activity.Id = CreateId(node, identityCounters, flattenedList);
            AssignInputOutputs(node.Activity);
            
            if(node.Activity is IVariableContainer variableContainer)
                AssignVariables(variableContainer);
        }
    }

    /// <inheritdoc />
    public void AssignInputOutputs(IActivity activity)
    {
        var inputs = activity.GetInputs();
        var seed = 0;

        foreach (var input in inputs)
        {
            var blockReference = input.MemoryBlockReference();
            
            if(string.IsNullOrEmpty(blockReference.Id))
                blockReference.Id = $"{activity.Id}:input-{++seed}";
        }

        seed = 0;
        var outputs = activity.GetOutputs();
        
        var assignedOutputs = outputs.Where(x =>
        {
            var memoryBlockReference = x.Value.MemoryBlockReference();
            return memoryBlockReference != null! && memoryBlockReference.Id == null!;
        }).ToList();

        foreach (var output in assignedOutputs)
        {
            var blockReference = output.Value.MemoryBlockReference();
            
            if(string.IsNullOrEmpty(blockReference.Id))
                blockReference.Id = $"{activity.Id}:output-{++seed}";
        }
    }

    /// <inheritdoc />
    public void AssignVariables(IVariableContainer activity)
    {
        var variables = activity.GetVariables();
        var seed = 0;

        foreach (var variable in variables)
            variable.Id = variable.Name != null! ? variable.Name : $"{activity.Id}:variable-{++seed}";
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