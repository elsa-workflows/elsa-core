using Elsa.Common.Models;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Microsoft.EntityFrameworkCore;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Modules.Management;

/// <summary>
/// An EF Core implementation of <see cref="IWorkflowInstanceStore"/>.
/// </summary>
public class EFCoreWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly EntityStore<ManagementElsaDbContext, WorkflowInstance> _store;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreWorkflowInstanceStore(EntityStore<ManagementElsaDbContext, WorkflowInstance> store, IWorkflowStateSerializer workflowStateSerializer)
    {
        _store = store;
        _workflowStateSerializer = workflowStateSerializer;
    }

    /// <inheritdoc />
    public async ValueTask<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(query => Filter(query, filter), OnLoadAsync, cancellationToken).FirstOrDefault();

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(query => Filter(query, filter), x => x.Id, cancellationToken).LongCount();
        var entities = await _store.QueryAsync(query => Filter(query, filter).Paginate(pageArgs), OnLoadAsync, cancellationToken).ToList();
        return Page.Of(entities, count);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(query => Filter(query, filter), x => x.Id, cancellationToken).LongCount();
        var entities = await _store.QueryAsync(query => Filter(query, filter).OrderBy(order).Paginate(pageArgs), OnLoadAsync, cancellationToken).ToList();
        return Page.Of(entities, count);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(query => Filter(query, filter), OnLoadAsync, cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(query => Filter(query, filter).OrderBy(order), OnLoadAsync, cancellationToken).ToList().AsEnumerable();
    
    /// <inheritdoc />
    public async ValueTask<long> CountAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(filter.Apply, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(query => Filter(query, filter), x => x.Id, cancellationToken).LongCount();
        var entities = await _store.QueryAsync<WorkflowInstanceSummary>(query => Filter(query, filter).Paginate(pageArgs), WorkflowInstanceSummary.FromInstanceExpression(), cancellationToken).ToList();
        return Page.Of(entities, count);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowInstances;
        var queryable = Filter(set.AsQueryable(), filter).OrderBy(order);
        var count = await queryable.LongCountAsync(cancellationToken);
        queryable = queryable.Paginate(pageArgs);
        var entities = await queryable.Select(WorkflowInstanceSummary.FromInstanceExpression()).ToListAsync(cancellationToken);

        return Page.Of(entities, count);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(query => Filter(query, filter), WorkflowInstanceSummary.FromInstanceExpression(), cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(query => Filter(query, filter).OrderBy(order), WorkflowInstanceSummary.FromInstanceExpression(), cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) => 
        await _store.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);

    /// <inheritdoc />
    public async ValueTask SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default) =>
        await _store.SaveAsync(instance, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default) =>
        await _store.SaveManyAsync(instances, OnSaveAsync, cancellationToken);

    private async ValueTask OnSaveAsync(ManagementElsaDbContext managementElsaDbContext, WorkflowInstance entity, CancellationToken cancellationToken)
    {
        var data = entity.WorkflowState;
        var json = await _workflowStateSerializer.SerializeAsync(data, cancellationToken);

        managementElsaDbContext.Entry(entity).Property("Data").CurrentValue = json;
    }

    private async ValueTask OnLoadAsync(ManagementElsaDbContext managementElsaDbContext, WorkflowInstance? entity, CancellationToken cancellationToken)
    {
        if (entity == null)
            return;

        var data = entity.WorkflowState;
        var json = (string?)managementElsaDbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json)) 
            data = await _workflowStateSerializer.DeserializeAsync(json, cancellationToken);

        entity.WorkflowState = data;
    }

    private static IQueryable<WorkflowInstance> Filter(IQueryable<WorkflowInstance> query, WorkflowInstanceFilter filter) => filter.Apply(query);
}