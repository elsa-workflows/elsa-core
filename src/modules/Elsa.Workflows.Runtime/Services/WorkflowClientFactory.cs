using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowClientFactory(IServiceProvider serviceProvider) : IWorkflowClientFactory
{
    /// <inheritdoc />
    public IWorkflowClient CreateClient(string workflowInstanceId)
    {
        return ActivatorUtilities.CreateInstance<LocalWorkflowClient>(serviceProvider);
    }
}