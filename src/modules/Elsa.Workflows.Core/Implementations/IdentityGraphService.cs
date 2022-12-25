using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class IdentityGraphService : IIdentityGraphService
{
    private readonly IActivityWalker _activityWalker;

    public IdentityGraphService(IActivityWalker activityWalker)
    {
        _activityWalker = activityWalker;
    }

    public async Task AssignIdentitiesAsync(Workflow workflow, CancellationToken cancellationToken = default) => await AssignIdentitiesAsync((IActivity)workflow, cancellationToken);

    public async Task AssignIdentitiesAsync(IActivity root, CancellationToken cancellationToken = default)
    {
        var graph = await _activityWalker.WalkAsync(root, cancellationToken);
        AssignIdentities(graph);
    }

    public void AssignIdentities(ActivityNode root)
    {
        var identityCounters = new Dictionary<string, int>();
        var list = root.Flatten().ToList();

        foreach (var node in list)
        {
            node.Activity.Id = CreateId(node, identityCounters, list);
            AssignInputOutputs(node.Activity);
            
            if(node.Activity is IVariableContainer variableContainer)
                AssignVariables(variableContainer);
        }
    }

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

    public void AssignVariables(IVariableContainer activity)
    {
        var variables = activity.GetVariables();
        var seed = 0;

        foreach (var variable in variables)
            variable.Id = variable.Name != null! ? variable.Name : $"{activity.Id}:variable-{++seed}";
    }

    private string CreateId(ActivityNode activityNode, IDictionary<string, int> identityCounters, ICollection<ActivityNode> allNodes)
    {
        if (!string.IsNullOrWhiteSpace(activityNode.NodeId))
            return activityNode.NodeId;

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