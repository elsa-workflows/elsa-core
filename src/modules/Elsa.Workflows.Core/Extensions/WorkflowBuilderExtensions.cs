using Elsa.Workflows;
using Elsa.Workflows.Activities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Contains extensions for <see cref="IWorkflowBuilder"/>.
/// </summary>
public static class WorkflowBuilderExtensions
{
    /// <summary>
    /// Builds a workflow asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the workflow.</typeparam>
    /// <param name="builder">The <see cref="IWorkflowBuilder"/> instance to build the workflow.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation that returns the built <see cref="Workflow"/>.</returns>
    public static Task<Workflow> BuildWorkflowAsync<T>(
        this IWorkflowBuilder builder, 
        CancellationToken cancellationToken = default) where T : IWorkflow, new()
    {
        return builder.BuildWorkflowAsync(Activator.CreateInstance<T>(), cancellationToken);
    }
}