using Elsa.Common.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management;

/// <summary>
/// Represents a store of workflow instances.
/// </summary>
/// <remarks>
/// Implementations must honor <see cref="WorkflowInstanceFilter.TenantAgnostic"/> on reads by ignoring the tenant filter.
/// Deletes stay scoped to the current tenant.
/// </remarks>
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

    /// <summary>
    /// Updates the "LastUpdated" timestamp of a workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The unique identifier of the workflow instance.</param>
    /// <param name="value">The new timestamp value to set.</param>
    /// <param name="cancellationToken">The cancellation token to observe during the operation.</param>
    Task UpdateUpdatedTimestampAsync(string workflowInstanceId, DateTimeOffset value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets <see cref="WorkflowInstance.SubStatus"/> to <see cref="WorkflowSubStatus.Interrupted"/> and
    /// <see cref="WorkflowInstance.IsExecuting"/> to <c>false</c> only if the stored instance is still
    /// <see cref="WorkflowStatus.Running"/>.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance to mark.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="allowFinishedCancelled">
    /// Unused by Elsa Core as of #8419. Kept so the 3.8.4 signature remains binary-compatible with
    /// extension-package implementations. Drain no longer passes <c>true</c>: a
    /// <see cref="WorkflowStatus.Finished"/> / <see cref="WorkflowSubStatus.Cancelled"/> row is never
    /// promoted to <see cref="WorkflowStatus.Running"/> + <see cref="WorkflowSubStatus.Interrupted"/>.
    /// Implementations should refuse every <see cref="WorkflowStatus.Finished"/> row regardless of this flag.
    /// </param>
    /// <returns>
    /// <c>true</c> when the interrupt markers were applied; <c>false</c> when the instance is missing,
    /// not visible to the current tenant, or already <see cref="WorkflowStatus.Finished"/>.
    /// Implementations must not overwrite a concurrent naturally completed commit.
    /// </returns>
    ValueTask<bool> TryMarkInterruptedAsync(string workflowInstanceId, CancellationToken cancellationToken = default, bool allowFinishedCancelled = false);

    /// <summary>
    /// Sets <see cref="WorkflowInstance.SubStatus"/> to <paramref name="next"/> and
    /// <see cref="WorkflowInstance.UpdatedAt"/> to <paramref name="stampedAt"/> when the stored instance is still
    /// <see cref="WorkflowStatus.Running"/> and still <paramref name="expected"/>.
    /// </summary>
    /// <remarks>
    /// When <paramref name="expectedUpdatedAt"/> has a value, <see cref="WorkflowInstance.UpdatedAt"/> must equal it.
    /// A transition to <see cref="WorkflowSubStatus.Cancelling"/> also requires the row not to be executing and
    /// <paramref name="expected"/> to be <see cref="WorkflowSubStatus.Suspended"/>.
    /// A transition out of <see cref="WorkflowSubStatus.Cancelling"/> may only return to <see cref="WorkflowSubStatus.Suspended"/>.
    /// Only those two columns are written. <see cref="WorkflowInstance.WorkflowState"/>, bookmarks and execution progress stay as they are.
    /// The default returns <c>false</c> so a provider that cannot do this conditional write fails closed.
    /// Callers must not fall back to <see cref="SaveAsync"/>, <see cref="SaveManyAsync"/> or <see cref="UpdateAsync"/>.
    /// </remarks>
    /// <param name="workflowInstanceId">The workflow instance to update.</param>
    /// <param name="expected">The sub-status the row must still have.</param>
    /// <param name="expectedUpdatedAt">The <see cref="WorkflowInstance.UpdatedAt"/> the row must still have, or <c>null</c> to skip that compare.</param>
    /// <param name="next">The sub-status to write.</param>
    /// <param name="stampedAt">The stamp written to <see cref="WorkflowInstance.UpdatedAt"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> when the row was updated; otherwise <c>false</c> and the row is unchanged.</returns>
    ValueTask<bool> TryTransitionSubStatusAsync(
        string workflowInstanceId,
        WorkflowSubStatus expected,
        DateTimeOffset? expectedUpdatedAt,
        WorkflowSubStatus next,
        DateTimeOffset stampedAt,
        CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
}