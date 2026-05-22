using Elsa.Expressions.Options;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime;

internal static class WorkflowRuntimeTypeAliasRegistrar
{
    public static void Register(ExpressionOptions options, IEnumerable<Type> workflowTypes)
    {
        options.RegisterTypeAlias(typeof(EventBookmarkPayload), nameof(EventBookmarkPayload));
        options.RegisterTypeAlias(typeof(ExecuteWorkflowPayload), nameof(ExecuteWorkflowPayload));
        options.RegisterTypeAlias(typeof(RunTaskBookmarkPayload), nameof(RunTaskBookmarkPayload));
        options.RegisterTypeAlias(typeof(BookmarkTokenPayload), nameof(BookmarkTokenPayload));
        options.RegisterTypeAlias(typeof(EventTokenPayload), nameof(EventTokenPayload));
        options.RegisterTypeAlias(typeof(ExecuteWorkflowResult), nameof(ExecuteWorkflowResult));
        options.RegisterTypeAlias(typeof(WorkflowInterruptedPayload), nameof(WorkflowInterruptedPayload));
        options.RegisterTypeAlias(typeof(BackgroundActivityStimulus), nameof(BackgroundActivityStimulus));
        options.RegisterTypeAlias(typeof(BulkDispatchWorkflowsStimulus), nameof(BulkDispatchWorkflowsStimulus));
        options.RegisterTypeAlias(typeof(DispatchWorkflowStimulus), nameof(DispatchWorkflowStimulus));
        options.RegisterTypeAlias(typeof(EventStimulus), nameof(EventStimulus));
        options.RegisterTypeAlias(typeof(ExecuteWorkflowStimulus), nameof(ExecuteWorkflowStimulus));
        options.RegisterTypeAlias(typeof(RunTaskStimulus), nameof(RunTaskStimulus));

        foreach (var workflowType in workflowTypes.Where(IsConcreteWorkflowType).Distinct())
            options.RegisterTypeAlias(workflowType, workflowType.GetSimpleAssemblyQualifiedName());
    }

    private static bool IsConcreteWorkflowType(Type type)
    {
        return typeof(IWorkflow).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false };
    }
}
