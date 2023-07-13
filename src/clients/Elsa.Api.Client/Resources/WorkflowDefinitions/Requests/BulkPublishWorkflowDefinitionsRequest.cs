namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;

/// <summary>
/// A request to publish many workflow definitions.
/// </summary>
public class BulkPublishWorkflowDefinitionsRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkPublishWorkflowDefinitionsRequest"/> class.
    /// </summary>
    /// <param name="definitionIds">The IDs of the workflow definitions to publish.</param>
    public BulkPublishWorkflowDefinitionsRequest(IEnumerable<string> definitionIds)
    {
        DefinitionIds = definitionIds.ToArray();
    }
    
    /// <summary>
    /// Gets or sets the IDs of the workflow definitions to delete.
    /// </summary>
    public string[] DefinitionIds { get; set; }
}