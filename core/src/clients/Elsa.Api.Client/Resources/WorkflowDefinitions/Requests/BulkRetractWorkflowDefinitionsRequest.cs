namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;

/// <summary>
/// A request to retract many workflow definitions.
/// </summary>
public class BulkRetractWorkflowDefinitionsRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkRetractWorkflowDefinitionsRequest"/> class.
    /// </summary>
    /// <param name="definitionIds">The IDs of the workflow definitions to retract.</param>
    public BulkRetractWorkflowDefinitionsRequest(IEnumerable<string> definitionIds)
    {
        DefinitionIds = definitionIds.ToArray();
    }
    
    /// <summary>
    /// Gets or sets the IDs of the workflow definitions to delete.
    /// </summary>
    public string[] DefinitionIds { get; set; }
}