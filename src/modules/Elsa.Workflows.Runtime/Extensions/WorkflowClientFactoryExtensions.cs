using System.Diagnostics.CodeAnalysis;
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
    public static IWorkflowClient CreateClient(this IWorkflowClientFactory factory, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type clientType, Workflow workflow, string? workflowInstanceId = null)
    {
        return factory.CreateClient(clientType, workflow.Identity.Id, workflowInstanceId);
    }

    /// <summary>
    /// Creates a new <see cref="IWorkflowClient"/> for the specified workflow.
    /// </summary>
    public static IWorkflowClient CreateClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IWorkflowClientFactory factory, Workflow workflow, string? workflowInstanceId = null) where T : IWorkflowClient
    {
        return factory.CreateClient(typeof(T), workflow, workflowInstanceId);
    }
}