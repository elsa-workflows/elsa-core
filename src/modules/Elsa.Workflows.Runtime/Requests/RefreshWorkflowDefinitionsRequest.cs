namespace Elsa.Workflows.Runtime.Requests;

/// <summary>
/// Represents a request to refresh workflow definitions by re-indexing their triggers.
/// </summary>
public class RefreshWorkflowDefinitionsRequest
{
    /// <summary>
    /// Initializes a new instance of <see cref="RefreshWorkflowDefinitionsRequest"/>.
    /// </summary>
    public RefreshWorkflowDefinitionsRequest()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RefreshWorkflowDefinitionsRequest"/>.
    /// </summary>
    public RefreshWorkflowDefinitionsRequest(IEnumerable<string>? definitionIds, int batchSize)
    {
        BatchSize = batchSize;
        DefinitionIds = definitionIds?.ToList();
    }
    
    /// <summary>
    /// Gets or sets the batch size for refreshing workflow definitions.
    /// The batch size determines the number of definitions to be processed at a time during the refresh process.
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the collection of definition IDs for workflow definitions to be refreshed.
    /// If not specified, all workflows will be refreshed.
    /// </summary>
    public ICollection<string>? DefinitionIds { get; set; }
}