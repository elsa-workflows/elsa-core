namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;

/// <summary>
/// A request to delete many workflow definitions.
/// </summary>
public class BulkDeleteWorkflowDefinitionsRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkDeleteWorkflowDefinitionsRequest"/> class.
    /// </summary>
    /// <param name="definitionIds">The IDs of the workflow definitions to delete.</param>
    public BulkDeleteWorkflowDefinitionsRequest(IEnumerable<string> definitionIds)
    {
        DefinitionIds = definitionIds.ToArray();
    }
    
    /// <summary>
    /// Gets or sets the IDs of the workflow definitions to delete.
    /// </summary>
    public string[] DefinitionIds { get; set; }
}