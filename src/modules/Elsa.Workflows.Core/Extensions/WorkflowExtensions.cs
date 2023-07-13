using Elsa.Workflows.Core.Activities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

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