namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;

/// <summary>
/// A request to delete many workflow definitions.
/// </summary>
public class BulkDeleteWorkflowDefinitionVersionsRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkDeleteWorkflowDefinitionVersionsRequest"/> class.
    /// </summary>
    /// <param name="ids">The IDs of the workflow definition versions to delete.</param>
    public BulkDeleteWorkflowDefinitionVersionsRequest(IEnumerable<string> ids)
    {
        Ids = ids.ToArray();
    }
    
    /// <summary>
    /// Gets or sets the IDs of the workflow definitions to delete.
    /// </summary>
    public string[] Ids { get; set; }
}