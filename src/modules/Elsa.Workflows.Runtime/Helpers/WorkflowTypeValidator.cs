using Elsa.Workflows;

namespace Elsa.Workflows.Runtime.Helpers;

internal static class WorkflowTypeValidator
{
    public static void Validate(Type workflowType)
    {
        if (!typeof(IWorkflow).IsAssignableFrom(workflowType))
            throw new ArgumentException($"Workflow type '{GetDisplayName(workflowType)}' must implement {nameof(IWorkflow)}.", nameof(workflowType));

        if (workflowType.IsAbstract || workflowType.IsInterface || workflowType.IsGenericTypeDefinition || workflowType.ContainsGenericParameters)
            throw new ArgumentException($"Workflow type '{GetDisplayName(workflowType)}' must be a concrete, closed type.", nameof(workflowType));
    }

    private static string GetDisplayName(Type type) => type.FullName ?? type.Name;
}
