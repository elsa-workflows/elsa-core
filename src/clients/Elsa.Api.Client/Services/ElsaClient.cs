using Elsa.Api.Client.Contracts;

namespace Elsa.Api.Client.Services;

/// <inheritdoc />
public class ElsaClient : IElsaClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ElsaClient"/> class.
    /// </summary>
    /// <param name="workflowDefinitionsApi">The workflow definitions API.</param>
    public ElsaClient(IWorkflowDefinitionsApi workflowDefinitionsApi)
    {
        WorkflowDefinitions = workflowDefinitionsApi;
    }

    /// <inheritdoc />
    public IWorkflowDefinitionsApi WorkflowDefinitions { get; }
}