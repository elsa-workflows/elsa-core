using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common.Models;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Extensions;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Modules.Management;

/// <summary>
/// An EF Core implementation of <see cref="IWorkflowInstanceStore"/>.
/// </summary>
public class EFCoreWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly EntityStore<ManagementElsaDbContext, WorkflowInstance> _store;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreWorkflowInstanceStore(EntityStore<ManagementElsaDbContext, WorkflowInstance> store, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) => 
        await _store.QueryAsync(query => Filter(query, filter), Load, cancellationToken).FirstOrDefault();

    /// <inheritdoc />
    public async Task<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(query => Filter(query, filter), x => x.Id, cancellationToken).LongCount();
        var entities = await _store.QueryAsync(query => Filter(query, filter).Paginate(pageArgs), Load, cancellationToken).ToList();
        return Page.Of(entities, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(query => Filter(query, filter), x => x.Id, cancellationToken).LongCount();
        var entities = await _store.QueryAsync(query => Filter(query, filter).OrderBy(order).Paginate(pageArgs), Load, cancellationToken).ToList();
        return Page.Of(entities, count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) => 
        await _store.QueryAsync(query => Filter(query, filter), cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) => 
        await _store.QueryAsync(query => Filter(query, filter).OrderBy(order), Load, cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(query => Filter(query, filter),x => x.Id, cancellationToken).LongCount();
        var entities = await _store.QueryAsync<WorkflowInstanceSummary>(query => Filter(query, filter).Paginate(pageArgs), x => WorkflowInstanceSummary.FromInstance(x), cancellationToken).ToList();
        return Page.Of(entities, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(query => Filter(query, filter), x => x.Id, cancellationToken).LongCount();
        var entities = await _store.QueryAsync(query => Filter(query, filter).OrderBy(order).Paginate(pageArgs), x => WorkflowInstanceSummary.FromInstance(x), cancellationToken).ToList();
        return Page.Of(entities, count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) => 
        await _store.QueryAsync(query => Filter(query, filter), x => WorkflowInstanceSummary.FromInstance(x), cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) => 
        await _store.QueryAsync(query => Filter(query, filter).OrderBy(order), x => WorkflowInstanceSummary.FromInstance(x), cancellationToken).ToList().AsEnumerable();

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var count = await DeleteManyAsync(filter, cancellationToken);
        return count > 0;
    }

    /// <inheritdoc />
    public async Task<int> DeleteManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var count = await _store.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);
        return count;
    }
    
    /// <inheritdoc />
    public async Task SaveAsync(WorkflowInstance record, CancellationToken cancellationToken = default) =>
        await _store.SaveAsync(record, Save, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowInstance> records, CancellationToken cancellationToken = default) =>
        await _store.SaveManyAsync(records, Save, cancellationToken);

    private WorkflowInstance Save(ManagementElsaDbContext managementElsaDbContext, WorkflowInstance entity)
    {
        var data = entity.WorkflowState;
        var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
        var json = JsonSerializer.Serialize(data, options);

        managementElsaDbContext.Entry(entity).Property("Data").CurrentValue = json;
        return entity;
    }

    private WorkflowInstance? Load(ManagementElsaDbContext managementElsaDbContext, WorkflowInstance? entity)
    {
        if (entity == null)
            return null;

        var data = entity.WorkflowState;
        var json = (string?)managementElsaDbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
            data = JsonSerializer.Deserialize<WorkflowState>(json, options)!;
        }

        entity.WorkflowState = data;

        return entity;
    }

    private static IQueryable<WorkflowInstance> Filter(IQueryable<WorkflowInstance> query, WorkflowInstanceFilter filter) => filter.Apply(query);
}