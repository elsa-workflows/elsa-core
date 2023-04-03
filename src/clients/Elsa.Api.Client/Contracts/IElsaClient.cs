namespace Elsa.Api.Client.Contracts;

/// <summary>
/// Represents a client for the Elsa API. Each API is exposed as a property.
/// </summary>
public interface IElsaClient
{
    /// <summary>
    /// Gets the workflow definitions API.
    /// </summary>
    IWorkflowDefinitionsApi WorkflowDefinitions { get; }
    
    /// <summary>
    /// Gets the workflow instances API.
    /// </summary>
    IWorkflowInstancesApi WorkflowInstances { get; }
}