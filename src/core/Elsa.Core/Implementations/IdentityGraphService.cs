using Elsa.Models;
using Elsa.Services;

namespace Elsa.Implementations;

public class IdentityGraphService : IIdentityGraphService
{
    private readonly IActivityWalker _activityWalker;

    public IdentityGraphService(IActivityWalker activityWalker)
    {
        _activityWalker = activityWalker;
    }

    public void AssignIdentities(Workflow workflow) => AssignIdentities(workflow.Root);

    public void AssignIdentities(IActivity root)
    {
        var graph = _activityWalker.Walk(root);
        AssignIdentities(graph);
    }

    public void AssignIdentities(ActivityNode root)
    {
        var identityCounters = new Dictionary<string, int>();
        var list = root.Flatten();

        foreach (var node in list)
        {
            node.Activity.Id = CreateId(node, identityCounters);
            AssignInputOutputs(node.Activity);
            AssignVariables(node.Activity);
        }
    }

    private void AssignInputOutputs(IActivity activity)
    {
        var inputs = activity.GetInputs();
        var assignedInputs = inputs.Where(x => x.LocationReference != null! && x.LocationReference.Id == null!).ToList();
        var seed = 0;

        foreach (var input in assignedInputs)
        {
            var locationReference = input.LocationReference;
            locationReference.Id = $"{activity.Id}:input-{++seed}";
        }

        seed = 0;
        var outputs = activity.GetOutputs();
        var assignedOutputs = outputs.Where(x => x.Value.LocationReference != null! && x.Value.LocationReference.Id == null!).ToList();

        foreach (var output in assignedOutputs)
        {
            var locationReference = output.Value.LocationReference;
            locationReference.Id = $"{activity.Id}:output-{++seed}";
        }
    }

    private void AssignVariables(IActivity activity)
    {
        var variables = activity.GetVariables();
        var seed = 0;

        foreach (var variable in variables) 
            variable.Id = variable.Name != null! ? variable.Name : $"{activity.Id}:variable-{++seed}";
    }

    private string CreateId(ActivityNode activityNode, IDictionary<string, int> identityCounters)
    {
        if (!string.IsNullOrWhiteSpace(activityNode.NodeId))
            return activityNode.NodeId;
        
        var fullTypeName = activityNode.Activity.TypeName;
        var shortTypeName = fullTypeName.Split('.').Last();
        var index = GetNextIndexFor(shortTypeName, identityCounters);
        var name = $"{shortTypeName}{index + 1}";
        return name;
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