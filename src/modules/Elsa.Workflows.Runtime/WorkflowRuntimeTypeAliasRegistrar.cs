using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Extensions;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime;

internal static class WorkflowRuntimeTypeAliasRegistrar
{
    public static void Register(WorkflowJsonTypeOptions options, IEnumerable<Type> workflowTypes)
    {
        options.AddTypeAlias<EventBookmarkPayload>();
        options.AddTypeAlias<ExecuteWorkflowPayload>();
        options.AddTypeAlias<RunTaskBookmarkPayload>();
        options.AddTypeAlias<BookmarkTokenPayload>();
        options.AddTypeAlias<EventTokenPayload>();
        options.AddTypeAlias<ExecuteWorkflowResult>();
        options.AddTypeAlias<WorkflowInterruptedPayload>();
        options.AddTypeAlias<BackgroundActivityStimulus>();
        options.AddTypeAlias<BulkDispatchWorkflowsStimulus>();
        options.AddTypeAlias<DispatchWorkflowStimulus>();
        options.AddTypeAlias<EventStimulus>();
        options.AddTypeAlias<ExecuteWorkflowStimulus>();
        options.AddTypeAlias<RunTaskStimulus>();

        foreach (var workflowType in workflowTypes.Where(IsConcreteWorkflowType).Distinct())
            options.RegisterTypeAlias(workflowType, workflowType.GetSimpleAssemblyQualifiedName());
    }

    private static bool IsConcreteWorkflowType(Type type)
    {
        return typeof(IWorkflow).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false };
    }
}
