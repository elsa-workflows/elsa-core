using System.Diagnostics.CodeAnalysis;
using Elsa.Common;
using Elsa.Common.Codecs;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.State;
using FastEndpoints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.Linq.AsyncExtensions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Elsa.Persistence.EFCore.Modules.Management;

/// <summary>
/// An EF Core implementation of <see cref="IWorkflowInstanceStore"/>.
/// </summary>
[UsedImplicitly]
public class EFCoreWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly EntityStore<ManagementElsaDbContext, WorkflowInstance> _store;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly ICompressionCodecResolver _compressionCodecResolver;
    private readonly IPayloadManager payloadManager;
    private readonly IOptions<ManagementOptions> _options;
    private readonly ILogger<EFCoreWorkflowInstanceStore> _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreWorkflowInstanceStore(
        EntityStore<ManagementElsaDbContext, WorkflowInstance> store,
        IWorkflowStateSerializer workflowStateSerializer,
        ICompressionCodecResolver compressionCodecResolver,
        IPayloadManager payloadManager,
        IOptions<ManagementOptions> options,
        ILogger<EFCoreWorkflowInstanceStore> logger)
    {
        _store = store;
        _workflowStateSerializer = workflowStateSerializer;
        _compressionCodecResolver = compressionCodecResolver;
        this.payloadManager = payloadManager;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(query => Filter(query, filter), OnLoadAsync, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var orderBy = new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending);
        return await FindManyAsync(filter, pageArgs, orderBy, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(query => Filter(query, filter), x => x.Id, cancellationToken).LongCount();
        var entities = await _store.QueryAsync(query => Filter(query, filter).OrderBy(order).Paginate(pageArgs), OnLoadAsync, cancellationToken).ToList();
        return Page.Of(entities, count);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var orderBy = new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending);
        return await FindManyAsync(filter, orderBy, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(query => Filter(query, filter).OrderBy(order), OnLoadAsync, cancellationToken).ToList().AsEnumerable();
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.SerializeAsync(WorkflowState, CancellationToken)")]
    public async ValueTask<long> CountAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(filter.Apply, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var orderBy = new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending);
        return await SummarizeManyAsync(filter, pageArgs, orderBy, cancellationToken);
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
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var orderBy = new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending);
        return await SummarizeManyAsync(filter, orderBy, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(query => Filter(query, filter).OrderBy(order), WorkflowInstanceSummary.FromInstanceExpression(), cancellationToken).ToList().AsEnumerable();
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = await _store.QueryAsync(query => Filter(query, filter).OrderBy(x => x.CreatedAt), WorkflowInstanceId.FromInstanceExpression(), cancellationToken).ToList().AsEnumerable();
        return entities.Select(x => x.Id).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<Page<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var orderBy = new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending);
        return await FindManyIdsAsync(filter, pageArgs, orderBy, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<string>> FindManyIdsAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowInstances;
        var queryable = Filter(set.AsQueryable(), filter).OrderBy(order);
        var count = await queryable.LongCountAsync(cancellationToken);
        queryable = queryable.Paginate(pageArgs);
        var entities = await queryable.Select(WorkflowInstanceId.FromInstanceExpression()).ToListAsync(cancellationToken);
        var ids = entities.Select(x => x.Id).ToList();

        return Page.Of(ids, count);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteWhereAsync(query => Filter(query, filter), cancellationToken);
    }

    public async Task UpdateUpdatedTimestampAsync(string workflowInstanceId, DateTimeOffset value, CancellationToken cancellationToken = default)
    {
        var entity = new WorkflowInstance
        {
            Id = workflowInstanceId,
            UpdatedAt = value
        };

        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        dbContext.Attach(entity);
        dbContext.Entry(entity).Property(x => x.UpdatedAt).IsModified = true;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException e)
        {
            foreach (var entry in e.Entries)
            {
                var proposedValues = entry.CurrentValues;
                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                
                if(databaseValues == null)
                    continue;
                
                var updatedAtProperty = entry.Metadata.GetProperty(nameof(WorkflowInstance.UpdatedAt));
                var proposedValue = (DateTimeOffset)proposedValues[updatedAtProperty]!;
                var databaseValue = (DateTimeOffset)databaseValues[updatedAtProperty]!;

                if (proposedValue > databaseValue)
                    proposedValues[updatedAtProperty] = proposedValue;

                entry.OriginalValues.SetValues(databaseValues);
            }
        }
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls Elsa.Workflows.Contracts.IWorkflowStateSerializer.SerializeAsync(WorkflowState, CancellationToken)")]
    public async ValueTask SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        await _store.SaveAsync(instance, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        await _store.AddAsync(instance, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask UpdateAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        await _store.UpdateAsync(instance, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default)
    {
        await _store.SaveManyAsync(instances, cancellationToken);
    }    

    private async ValueTask OnLoadAsync(ManagementElsaDbContext managementElsaDbContext, WorkflowInstance? entity, CancellationToken cancellationToken)
    {
        if (entity == null)
            return;
        
        var json = await GetPayloadData(managementElsaDbContext, entity, cancellationToken);

        try
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                entity.WorkflowState = _workflowStateSerializer.Deserialize(json);
            }
        }
        catch (Exception exp)
        {
            _logger.LogWarning(exp, "Exception while deserializing workflow instance state: {InstanceId}. Reverting to default state", entity.Id);
        }        
    }

    private ValueTask<string?> GetPayloadData(ElsaDbContextBase dbContext, WorkflowInstance entity, CancellationToken cancellationToken)
    {
        var result = (string?)dbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(result) || entity.DataReference is null)
        {
            return new(result);
        }

        return payloadManager.Get(entity.DataReference, cancellationToken);
    }

    private static IQueryable<WorkflowInstance> Filter(IQueryable<WorkflowInstance> query, WorkflowInstanceFilter filter)
    {
        return filter.Apply(query);
    }
}