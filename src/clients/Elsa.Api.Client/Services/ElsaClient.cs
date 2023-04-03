using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Services;

/// <inheritdoc />
public class ElsaClient : IElsaClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElsaClient"/> class.
    /// </summary>
    public ElsaClient(IWorkflowDefinitionsApi workflowDefinitions, IWorkflowInstancesApi workflowInstances)
    {
        WorkflowDefinitions = workflowDefinitions;
        WorkflowInstances = workflowInstances;
    }

    /// <inheritdoc />
    public IWorkflowDefinitionsApi WorkflowDefinitions { get; }

    /// <inheritdoc />
    public IWorkflowInstancesApi WorkflowInstances { get; }
}