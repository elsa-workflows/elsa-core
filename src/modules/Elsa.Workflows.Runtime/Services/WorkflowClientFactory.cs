using System.Diagnostics.CodeAnalysis;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowClientFactory(IIdentityGenerator identityGenerator, IServiceProvider serviceProvider) : IWorkflowClientFactory
{
    /// <inheritdoc />
    public IWorkflowClient CreateClient([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]Type clientType, string workflowDefinitionVersionId, string? workflowInstanceId = null)
    {
        var client = (IWorkflowClient)ActivatorUtilities.CreateInstance(serviceProvider, clientType);
        client.WorkflowDefinitionVersionId = workflowDefinitionVersionId;
        client.WorkflowInstanceId = workflowInstanceId ?? identityGenerator.GenerateId();
        return client;
    }
}