namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// A request to delete many workflow definitions.
/// </summary>
public class DeleteManyWorkflowDefinitionsRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteManyWorkflowDefinitionsRequest"/> class.
    /// </summary>
    /// <param name="definitionIds">The IDs of the workflow definitions to delete.</param>
    public DeleteManyWorkflowDefinitionsRequest(IEnumerable<string> definitionIds)
    {
        DefinitionIds = definitionIds.ToArray();
    }
    
    /// <summary>
    /// Gets or sets the IDs of the workflow definitions to delete.
    /// </summary>
    public string[] DefinitionIds { get; set; }
}