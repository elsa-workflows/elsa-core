using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class WorkflowDefinitionBuilderExtensions
{
    public static Task<Workflow> BuildWorkflowAsync<T>(this IWorkflowBuilder builder, CancellationToken cancellationToken = default) where T : IWorkflow => builder.BuildWorkflowAsync(Activator.CreateInstance<T>(), cancellationToken);
}