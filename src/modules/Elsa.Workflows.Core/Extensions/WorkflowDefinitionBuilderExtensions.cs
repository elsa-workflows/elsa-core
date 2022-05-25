using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core;

public static class WorkflowDefinitionBuilderExtensions
{
    public static Task<Workflow> BuildWorkflowAsync<T>(this IWorkflowDefinitionBuilder builder, CancellationToken cancellationToken = default) where T : IWorkflow => builder.BuildWorkflowAsync(Activator.CreateInstance<T>(), cancellationToken);
}