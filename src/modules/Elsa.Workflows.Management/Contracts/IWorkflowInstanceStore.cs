using Elsa.Common.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management;

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
    ValueTask<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of workflow instances matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of workflow instances.</returns>
    ValueTask<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of workflow instances matching the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="order">The order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the property to order by.</typeparam>
    /// <returns>A paginated list of workflow instances.</returns>
    ValueTask<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a list of workflow instances matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of workflow instances.</returns>
    ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a list of workflow instances matching the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="order">The order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the property to order by.</typeparam>
    /// <returns>A list of workflow instances.</returns>
    ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count the number of workflow instances matching the specified filter. 
    /// </summary>
    ValueTask<long> CountAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of workflow instance summaries matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of workflow instance summaries.</returns>
    ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of workflow instance summaries matching the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="order">The order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the property to order by.</typeparam>
    /// <returns>A paginated list of workflow instance summaries.</returns>
    ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a list of workflow instance IDs matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of workflow instance IDs.</returns>
    ValueTask<IEnumerable<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of workflow instance IDs matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of workflow instance IDs.</returns>
    ValueTask<Page<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of workflow instance IDs matching the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="pageArgs">The page arguments.</param>
    /// <param name="order">The order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the property to order by.</typeparam>
    /// <returns>A paginated list of workflow instance IDs.</returns>
    ValueTask<Page<string>> FindManyIdsAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a list of workflow instance summaries matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of workflow instance summaries.</returns>
    ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a list of workflow instance summaries matching the specified filter and order.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="order">The order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TOrder">The type of the property to order by.</typeparam>
    /// <returns>A list of workflow instance summaries.</returns>
    ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrder>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrder> order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates the specified <see cref="WorkflowInstance"/> in the persistence store.
    /// </summary>
    /// <remarks>
    /// If the record does not already exist, it is added to the store; if it does exist, its existing entry is updated.
    /// </remarks>
    /// <param name="instance">The workflow instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds the specified <see cref="WorkflowInstance"/> in the persistence store.
    /// </summary>
    /// <param name="instance">The workflow instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the specified <see cref="WorkflowInstance"/> in the persistence store.
    /// </summary>
    /// <param name="instance">The workflow instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask UpdateAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates the specified set of <see cref="WorkflowInstance"/> objects in the persistence store.
    /// </summary>
    /// <param name="instances">The workflow instances.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// If the record does not already exist, it is added to the store; if it does exist, its existing entry is updated.
    /// </remarks>
    ValueTask SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all workflow instances matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of deleted workflow instances.</returns>
    ValueTask<long> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
}