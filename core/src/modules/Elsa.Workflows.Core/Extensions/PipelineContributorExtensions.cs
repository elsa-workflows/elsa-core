using Elsa.Workflows;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions;

internal static class PipelineContributorExtensions
{
    public static IWorkflowExecutionPipelineBuilder UseWorkflowExecutionPipelineContributors(this IWorkflowExecutionPipelineBuilder builder, IServiceProvider serviceProvider)
    {
        foreach (var contributor in serviceProvider.GetServices<IWorkflowExecutionPipelineContributor>().OrderBy(x => x.Order))
            contributor.Configure(builder);

        return builder;
    }

    public static IActivityExecutionPipelineBuilder UseActivityExecutionPipelineContributors(this IActivityExecutionPipelineBuilder builder, IServiceProvider serviceProvider)
    {
        foreach (var contributor in serviceProvider.GetServices<IActivityExecutionPipelineContributor>().OrderBy(x => x.Order))
            contributor.Configure(builder);

        return builder;
    }
}
