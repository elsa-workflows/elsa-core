using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <inheritdoc />
public class OutputBindingDestinationResolver : IOutputBindingDestinationResolver
{
    /// <inheritdoc />
    public OutputBindingDestination? Resolve(ActivityExecutionContext activityExecutionContext, Output output)
    {
        var reference = output.MemoryBlockReference();

        if (reference.Id == null)
            return null;

        if (activityExecutionContext.ExpressionExecutionContext.TryGetBlock(reference, out var block) &&
            block.Metadata is VariableBlockMetadata variableMetadata)
        {
            return CreateVariableDestination(variableMetadata.Variable);
        }

        return CreateWorkflowOutputDestination(activityExecutionContext.WorkflowExecutionContext.Workflow, reference.Id);
    }

    /// <inheritdoc />
    public OutputBindingDestination? Resolve(WorkflowGraph workflowGraph, ActivityNode activityNode, Output output)
    {
        var referenceId = output.MemoryBlockReference().Id;

        if (referenceId == null)
            return null;

        var variable = new[] { activityNode }
            .Concat(activityNode.Ancestors())
            .Select(x => x.Activity)
            .OfType<IVariableContainer>()
            .SelectMany(x => x.Variables)
            .FirstOrDefault(x => x.Id == referenceId);

        if (variable != null)
            return CreateVariableDestination(variable);

        return CreateWorkflowOutputDestination(workflowGraph.Workflow, referenceId);
    }

    private static OutputBindingDestination? CreateVariableDestination(Variable variable)
    {
        var variableType = variable.GetType().GenericTypeArguments.FirstOrDefault();
        return variableType == null
            ? null
            : new(variable.Id, variableType, AllowsNull(variableType), OutputBindingDestinationKind.Variable);
    }

    private static OutputBindingDestination? CreateWorkflowOutputDestination(Workflow workflow, string referenceId)
    {
        var output = workflow.Outputs.FirstOrDefault(x => x.Name == referenceId);
        return output == null
            ? null
            : new(output.Name, output.Type, AllowsNull(output.Type), OutputBindingDestinationKind.WorkflowOutput);
    }

    private static bool AllowsNull(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
}
