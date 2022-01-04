using Elsa.Contracts;
using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Services;

public class IdentityGraphService : IIdentityGraphService
{
    private readonly IActivityWalker _activityWalker;

    public IdentityGraphService(IActivityWalker activityWalker)
    {
        _activityWalker = activityWalker;
    }

    public void AssignIdentities(Workflow workflow)
    {
        AssignIdentities(workflow.Root);

        var triggers = workflow.Triggers;

        if (triggers == null)
            return;

        foreach (var trigger in triggers)
        {
            if(trigger is IActivity activity)
                AssignIdentities(activity);
        }
    }
        
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
        var assignedOutputs = outputs.Where(x => x.LocationReference != null! && x.LocationReference.Id == null!).ToList();

        foreach (var output in assignedOutputs)
        {
            var locationReference = output.LocationReference;
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

        var type = activityNode.Activity.NodeType;
        var index = GetNextIndexFor(type, identityCounters);
        var name = $"{Camelize(type)}{index + 1}";
        return name;
    }

    private int GetNextIndexFor(string nodeType, IDictionary<string, int> identityCounters)
    {
        if (!identityCounters.TryGetValue(nodeType, out var index))
        {
            identityCounters[nodeType] = index;
        }
        else
        {
            index = identityCounters[nodeType] + 1;
            identityCounters[nodeType] = index;
        }

        return index;
    }

    private string Camelize(string symbol) => char.ToLowerInvariant(symbol[0]) + symbol[1..];
}