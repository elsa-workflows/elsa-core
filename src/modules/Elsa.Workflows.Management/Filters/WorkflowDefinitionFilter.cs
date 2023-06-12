using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management.Entities;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Filters;

/// <summary>
/// A specification to use when finding workflow definitions. Only non-null fields will be included in the conditional expression.
/// </summary>
[PublicAPI]
public class WorkflowDefinitionFilter
{
    /// <summary>
    /// Filter by the ID of the workflow definition.
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// Filter by the IDs of the workflow definitions.
    /// </summary>
    public ICollection<string>? Ids { get; set; }
    
    /// <summary>
    /// Filter by the ID of the workflow definition.
    /// </summary>
    public string? DefinitionId { get; set; }
    
    /// <summary>
    /// Filter by the IDs of the workflow definitions.
    /// </summary>
    public ICollection<string>? DefinitionIds { get; set; }
    
    /// <summary>
    /// Filter by the version options.
    /// </summary>
    public VersionOptions? VersionOptions { get; set; }
    
    /// <summary>
    /// Filter by the name of the workflow definition.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter by the name or id of the workflow definition.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by the names of the workflow definitions.
    /// </summary>
    public ICollection<string>? Names { get; set; }
    
    /// <summary>
    /// Filter by the name of the workflow definition materializer.
    /// </summary>
    public string? MaterializerName { get; set; }
    
    /// <summary>
    /// Filter on workflows that are usable as activities.
    /// </summary>
    public bool? UsableAsActivity { get; set; }

    /// <summary>
    /// Applies the filter to the specified queryable.
    /// </summary>
    /// <param name="queryable">The queryable to apply the filter to.</param>
    /// <returns>The filtered queryable.</returns>
    public IQueryable<WorkflowDefinition> Apply(IQueryable<WorkflowDefinition> queryable)
    {
        var filter = this;
        if (filter.DefinitionId != null) queryable = queryable.Where(x => x.DefinitionId == filter.DefinitionId);
        if (filter.DefinitionIds != null) queryable = queryable.Where(x => filter.DefinitionIds.Contains(x.DefinitionId));
        if (filter.Id != null) queryable = queryable.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) queryable = queryable.Where(x => filter.Ids.Contains(x.Id));
        if (filter.VersionOptions != null) queryable = queryable.WithVersion(filter.VersionOptions.Value);
        if (filter.MaterializerName != null) queryable = queryable.Where(x => x.MaterializerName == filter.MaterializerName);
        if (filter.Name != null) queryable = queryable.Where(x => x.Name == filter.Name);
        if (filter.Names != null) queryable = queryable.Where(x => filter.Names.Contains(x.Name!));
        if (filter.UsableAsActivity != null) queryable = queryable.Where(x => x.Options.UsableAsActivity == filter.UsableAsActivity);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm)) queryable = queryable.Where(x => x.Name.Contains(filter.SearchTerm) || x.Description.Contains(filter.SearchTerm) || x.Id == filter.SearchTerm || x.DefinitionId == filter.SearchTerm);

        return queryable;
    }
}