using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowClientFactory(IIdentityGenerator identityGenerator, IServiceProvider serviceProvider) : IWorkflowClientFactory
{
    /// <inheritdoc />
    public IWorkflowClient CreateClient(string workflowDefinitionVersionId, string? workflowInstanceId = null)
    {
        var client = ActivatorUtilities.CreateInstance<LocalWorkflowClient>(serviceProvider);
        client.WorkflowDefinitionVersionId = workflowDefinitionVersionId;
        client.WorkflowInstanceId = workflowInstanceId ?? identityGenerator.GenerateId();
        return client;
    }
}