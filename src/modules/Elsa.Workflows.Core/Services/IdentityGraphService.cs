using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class IdentityGraphService : IIdentityGraphService
{
    private readonly IActivityVisitor _activityVisitor;
    private readonly IActivityRegistry _activityRegistry;

    /// <summary>
    /// Constructor.
    /// </summary>
    public IdentityGraphService(IActivityVisitor activityVisitor, IActivityRegistry activityRegistry)
    {
        _activityVisitor = activityVisitor;
        _activityRegistry = activityRegistry;
    }

    /// <inheritdoc />
    public async Task AssignIdentitiesAsync(Workflow workflow, CancellationToken cancellationToken = default) => await AssignIdentitiesAsync((IActivity)workflow, cancellationToken);

    /// <inheritdoc />
    public async Task AssignIdentitiesAsync(IActivity root, CancellationToken cancellationToken = default)
    {
        var graph = await _activityVisitor.VisitAsync(root, cancellationToken);
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
        var activityDescriptor = _activityRegistry.Find(activity.Type, activity.Version) ?? throw new Exception("Activity descriptor not found");
        var inputs = activityDescriptor.GetWrappedInputProperties(activity).Values.Where(x => x != null).Cast<Input>().ToList();
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