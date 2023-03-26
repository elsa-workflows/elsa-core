namespace Elsa.Api.Client.Contracts;

/// <summary>
/// Represents a client for the Elsa API.
/// </summary>
public interface IElsaClient
{
    /// <summary>
    /// Gets the workflow definitions API.
    /// </summary>
    IWorkflowDefinitionsApi WorkflowDefinitions { get; }
}