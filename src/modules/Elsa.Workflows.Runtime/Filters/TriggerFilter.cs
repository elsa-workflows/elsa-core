using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Filters;

/// <summary>
/// Represents a filter for triggers.
/// </summary>
public class TriggerFilter
{
    /// <summary>
    /// Gets or sets the ID of the trigger to find.
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// Gets or sets the IDs of the triggers to find.
    /// </summary>
    public ICollection<string>? Ids { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the workflow definition.
    /// </summary>
    public string? WorkflowDefinitionId { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the workflow definition version.
    /// </summary>
    public string? WorkflowDefinitionVersionId { get; set; }
    
    /// <summary>
    /// Gets or sets the IDs of the workflow definitions.
    /// </summary>
    public ICollection<string>? WorkflowDefinitionIds { get; set; }
    
    /// <summary>
    /// Gets or sets the IDs of the workflow definition versions.
    /// </summary>
    public ICollection<string>? WorkflowDefinitionVersionIds { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the trigger to find.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the names of the triggers to find.
    /// </summary>
    public ICollection<string>? Names { get; set; }
    
    /// <summary>
    /// Gets or sets the hash of the trigger to find.
    /// </summary>
    public string? Hash { get; set; }

    /// <summary>
    /// Applies the filter to the specified query.
    /// </summary>
    public IQueryable<StoredTrigger> Apply(IQueryable<StoredTrigger> queryable)
    {
        if (Id != null) queryable = queryable.Where(x => x.Id == Id);
        if (Ids != null) queryable = queryable.Where(x => Ids.Contains(x.Id));
        if (WorkflowDefinitionId != null) queryable = queryable.Where(x => x.WorkflowDefinitionId == WorkflowDefinitionId);
        if (WorkflowDefinitionIds != null) queryable = queryable.Where(x => WorkflowDefinitionIds.Contains(x.WorkflowDefinitionId));
        if (WorkflowDefinitionVersionId != null) queryable = queryable.Where(x => x.WorkflowDefinitionVersionId == WorkflowDefinitionVersionId);
        if (WorkflowDefinitionVersionIds != null) queryable = queryable.Where(x => WorkflowDefinitionVersionIds.Contains(x.WorkflowDefinitionVersionId));
        if (Name != null) queryable = queryable.Where(x => x.Name == Name);
        if (Names != null) queryable = queryable.Where(x => Names.Contains(x.Name));
        if (Hash != null) queryable = queryable.Where(x => x.Hash == Hash);
        return queryable;
    }
}