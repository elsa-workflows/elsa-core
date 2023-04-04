namespace Elsa.Api.Client.Contracts;

/// <summary>
/// The request for deleting a workflow definition.
/// </summary>
public class DeleteWorkflowDefinitionRequest
{
    /// <summary>
    /// The ID of the workflow definition to delete.
    /// </summary>
    public string DefinitionId { get; set; }
}