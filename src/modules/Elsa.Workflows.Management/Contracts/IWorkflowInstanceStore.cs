using Elsa.Common.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Represents a store of workflow instances.
/// </summary>
[PublicAPI]
public interface IWorkflowInstanceStore
{
    /// <summary>
    /// Finds a workflow instance matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The workflow instance.</returns>
    Task<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a paginated list of workflow instances matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of workflow instances.</returns>
    Task<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a paginated list of workflow instances matching the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="order">The order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the property to order by.</typeparam>
    /// <returns>A paginated list of workflow instances.</returns>
    Task<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a list of workflow instances matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of workflow instances.</returns>
    Task<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a list of workflow instances matching the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="order">The order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the property to order by.</typeparam>
    /// <returns>A list of workflow instances.</returns>
    Task<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a paginated list of workflow instance summaries matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of workflow instance summaries.</returns>
    Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a paginated list of workflow instance summaries matching the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="order">The order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the property to order by.</typeparam>
    /// <returns>A paginated list of workflow instance summaries.</returns>
    Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a list of workflow instance summaries matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of workflow instance summaries.</returns>
    Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a list of workflow instance summaries matching the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="order">The order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrder">The type of the property to order by.</typeparam>
    /// <returns>A list of workflow instance summaries.</returns>
    Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrder>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrder> order, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves the specified workflow instance.
    /// </summary>
    /// <param name="instance">The workflow instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves the specified workflow instances.
    /// </summary>
    /// <param name="instances">The workflow instances.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes all workflow instances matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of deleted workflow instances.</returns>
    Task<long> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
}