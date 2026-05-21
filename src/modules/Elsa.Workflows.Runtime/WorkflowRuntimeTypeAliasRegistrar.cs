using Elsa.Expressions.Options;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime;

internal static class WorkflowRuntimeTypeAliasRegistrar
{
    public static void Register(ExpressionOptions options, IEnumerable<string> workflowTypeNames)
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

        foreach (var workflowType in workflowTypeNames.Select(ResolveWorkflowType).OfType<Type>().Distinct())
            options.RegisterTypeAlias(workflowType, workflowType.GetSimpleAssemblyQualifiedName());
    }

    private static Type? ResolveWorkflowType(string workflowTypeName)
    {
        return Type.GetType(workflowTypeName, throwOnError: false) ??
               AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType(workflowTypeName, throwOnError: false)).FirstOrDefault(x => x != null);
    }
}
