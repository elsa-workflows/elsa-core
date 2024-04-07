using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Contracts;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IWorkflowClientFactory"/>.
/// </summary>
public static class WorkflowClientFactoryExtensions
{
    /// <summary>
    /// Creates a new <see cref="IWorkflowClient"/> for the specified workflow.
    /// </summary>
    public static IWorkflowClient CreateClient(this IWorkflowClientFactory factory, Workflow workflow, string? workflowInstanceId = null)
    {
        return factory.CreateClient(workflow.Identity.Id, workflowInstanceId);
    }
}