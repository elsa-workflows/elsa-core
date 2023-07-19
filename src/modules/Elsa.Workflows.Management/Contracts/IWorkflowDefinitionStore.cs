using Elsa.Common.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Represents a store of <see cref="WorkflowDefinition"/>s.
/// </summary>
public interface IWorkflowDefinitionStore
{
    /// <summary>
    /// Finds a workflow definition using the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The workflow definition.</returns>
    Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a workflow definition using the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="order">The order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the property to order by.</typeparam>
    /// <returns>The workflow definition.</returns>
    Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of workflow definitions using the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of workflow definitions.</returns>
    Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of workflow definitions using the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="order">The order.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the property to order by.</typeparam>
    /// <returns>A paginated list of workflow definitions.</returns>
    Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a list of workflow definitions using the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of workflow definitions.</returns>
    Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a list of workflow definitions using the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="order">The order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the property to order by.</typeparam>
    /// <returns>A list of workflow definitions.</returns>
    Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of workflow definition summaries using the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of workflow definition summaries.</returns>
    Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of workflow definition summaries using the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="order">The order.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the property to order by.</typeparam>
    /// <returns>A paginated list of workflow definition summaries.</returns>
    Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a list of workflow definition summaries using the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of workflow definition summaries.</returns>
    Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a list of workflow definition summaries using the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="order">The order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the property to order by.</typeparam>
    /// <returns>A list of workflow definition summaries.</returns>
    Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the latest version of the workflow definition matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The workflow definition.</returns>
    Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the specified workflow definition.
    /// </summary>
    /// <param name="definition">The workflow definition.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the specified workflow definitions.
    /// </summary>
    /// <param name="definitions">The workflow definitions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all workflow definitions matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of workflow definitions deleted.</returns>
    Task<long> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if any workflow definition matches the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if any workflow definition matches the specified filter.</returns>
    Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns the number of logical workflow definitions.
    /// </summary>
    Task<long> CountDistinctAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether the specified name is unique.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="definitionId">The definition ID to exclude from the check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<bool> GetIsNameUnique(string name, string? definitionId = default, CancellationToken cancellationToken = default);
}