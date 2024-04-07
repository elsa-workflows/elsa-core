using System.Diagnostics.CodeAnalysis;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowClientFactory(IIdentityGenerator identityGenerator, IServiceProvider serviceProvider) : IWorkflowClientFactory
{
    /// <inheritdoc />
    public async Task<IWorkflowClient> CreateClientAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type clientType,
        string workflowDefinitionVersionId,
        string? workflowInstanceId = null,
        CancellationToken cancellationToken = default)
    {
        var client = (IWorkflowClient)ActivatorUtilities.CreateInstance(serviceProvider, clientType);
        workflowInstanceId ??= identityGenerator.GenerateId();
        await client.InitializeAsync(workflowDefinitionVersionId, workflowInstanceId, cancellationToken);
        return client;
    }
}