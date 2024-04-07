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
    public static Task<IWorkflowClient> CreateClientAsync(this IWorkflowClientFactory factory,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type clientType,
        Workflow workflow,
        string? workflowInstanceId = null,
        CancellationToken cancellationToken = default)
    {
        return factory.CreateClientAsync(clientType, workflow.Identity.Id, workflowInstanceId, cancellationToken);
    }

    /// <summary>
    /// Creates a new <see cref="IWorkflowClient"/> for the specified workflow.
    /// </summary>
    public static Task<IWorkflowClient> CreateClientAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IWorkflowClientFactory factory,
        Workflow workflow,
        string? workflowInstanceId = null,
        CancellationToken cancellationToken = default) where T : IWorkflowClient
    {
        return factory.CreateClientAsync(typeof(T), workflow, workflowInstanceId, cancellationToken);
    }
}