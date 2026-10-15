using Elsa.Common.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management;

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
    /// Adds or updates the specified <see cref="WorkflowDefinition"/> in the persistence store.
    /// </summary>
    /// <param name="definition">The workflow definition.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// If the record does not already exist, it is added to the store; if it does exist, its existing entry is updated.
    /// </remarks>
    Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compare-and-swap the latest definition matching <paramref name="filter"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Loads the matching row, and only if <paramref name="matchesExpected"/> is true applies
    /// <paramref name="update"/> to that just-loaded row and saves the result. The load, the match,
    /// the update and the save are one critical section (memory) or one conditional write (EF:
    /// <c>ExecuteUpdate</c> against the loaded snapshot). A lost race returns
    /// <see cref="WorkflowDefinitionUpdateOutcome.Conflict"/> — it does not wait.
    /// </para>
    /// <para>
    /// <paramref name="update"/> sees the definition as stored at the moment of the swap, so
    /// metadata copied from it (name, variables, options, custom properties) is current — not a
    /// snapshot taken by the caller before this call. Treat that argument as read-only and return
    /// a new or cloned definition; mutating it in place can tear a shared in-memory instance.
    /// </para>
    /// <para>
    /// When <paramref name="update"/> returns a definition with a different <c>Id</c> (a new draft
    /// of a published version), the previously latest row is unmarked in the same step.
    /// </para>
    /// <para>
    /// Persistence providers outside this repository (Mongo, Dapper, Event Sourcing) must implement
    /// this the same way before the BPMN document <c>PUT</c> is concurrency-safe on those stores.
    /// Until they do, that endpoint's atomic precondition is not available there.
    /// </para>
    /// </remarks>
    /// <param name="filter">Typically the latest version of one definition id.</param>
    /// <param name="matchesExpected">
    /// True when the loaded row is still the snapshot the caller is allowed to overwrite
    /// (for the document <c>PUT</c>, the current ETag still equals <c>If-Match</c>).
    /// </param>
    /// <param name="update">Builds the definition to save from the just-loaded row.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<WorkflowDefinitionUpdateResult> TryUpdateLatestAsync(
        WorkflowDefinitionFilter filter,
        Func<WorkflowDefinition, bool> matchesExpected,
        Func<WorkflowDefinition, WorkflowDefinition> update,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds the specified set of <see cref="WorkflowDefinition"/> objects to te persistence store.
    /// </summary>
    /// <param name="definitions">The workflow definitions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>
    /// If the record does not already exist, it is added to the store; if it does exist, its existing entry is updated.
    /// </remarks>
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
    Task<bool> GetIsNameUnique(string name, string? definitionId = null, CancellationToken cancellationToken = default);
}