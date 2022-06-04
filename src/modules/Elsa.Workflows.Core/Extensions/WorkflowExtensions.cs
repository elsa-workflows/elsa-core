using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core;

public static class WorkflowExtensions
{
    public static Workflow WithVersion(this Workflow workflow, int version)
    {
        workflow.Identity = workflow.Identity with { Version = version };
        return workflow;
    }

    public static Workflow IncrementVersion(this Workflow workflow) => WithVersion(workflow, workflow.Identity.Version + 1);

    public static Workflow WithPublished(this Workflow workflow, bool value = true)
    {
        workflow.Publication = workflow.Publication with { IsPublished = value };
        return workflow;
    }

    public static Workflow WithLatest(this Workflow workflow, bool value = true)
    {
        workflow.Publication = workflow.Publication with { IsLatest = value };
        return workflow;
    }

    public static Workflow WithId(this Workflow workflow, string value)
    {
        workflow.Identity = workflow.Identity with { Id = value };
        return workflow;
    }

    public static Workflow WithDefinitionId(this Workflow workflow, string value)
    {
        workflow.Identity = workflow.Identity with { DefinitionId = value };
        return workflow;
    }
}

public static class MemoryDatumReferenceExtensions
{
    public static object? Get(this MemoryDatumReference reference, ActivityExecutionContext context) => reference.Get(context.ExpressionExecutionContext);
    public static T? Get<T>(this MemoryDatumReference reference, ActivityExecutionContext context) => (T?)reference.Get(context.ExpressionExecutionContext);
    public static void Set(this MemoryDatumReference reference, ActivityExecutionContext context, object? value) => reference.Set(context.ExpressionExecutionContext, value);
}