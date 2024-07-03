namespace Elsa.Workflows.Runtime.Requests;

/// Represents a request to refresh workflow definitions by re-indexing their triggers.
public class RefreshWorkflowDefinitionsRequest
{
    /// <summary>
    /// 
    /// </summary>
    public RefreshWorkflowDefinitionsRequest()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public RefreshWorkflowDefinitionsRequest(IEnumerable<string>? definitionIds, int batchSize)
    {
        BatchSize = batchSize;
        DefinitionIds = definitionIds?.ToList();
    }
    
    /// Gets or sets the batch size for refreshing workflow definitions.
    /// The batch size determines the number of definitions to be processed at a time during the refresh process.
    public int BatchSize { get; set; } = 10;

    /// Gets or sets the collection of definition IDs for workflow definitions to be refreshed.
    /// If not specified, all workflows will be refreshed.
    public ICollection<string>? DefinitionIds { get; set; }
}