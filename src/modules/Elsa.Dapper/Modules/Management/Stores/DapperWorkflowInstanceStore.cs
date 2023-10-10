using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Management.Records;
using Elsa.Dapper.Services;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Dapper.Modules.Management.Stores;

/// <summary>
/// Provides a Dapper implementation of <see cref="IWorkflowInstanceStore"/>.
/// </summary>
public class DapperWorkflowInstanceStore : IWorkflowInstanceStore
{
    private const string TableName = "WorkflowInstances";
    private const string PrimaryKeyName = "Id";
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly Store<WorkflowInstanceRecord> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperWorkflowInstanceStore"/> class.
    /// </summary>
    public DapperWorkflowInstanceStore(IDbConnectionProvider dbConnectionProvider, IWorkflowStateSerializer workflowStateSerializer)
    {
        _workflowStateSerializer = workflowStateSerializer;
        _store = new Store<WorkflowInstanceRecord>(dbConnectionProvider, TableName, PrimaryKeyName);
    }

    /// <inheritdoc />
    public async ValueTask<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await _store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record == null ? null : await MapAsync(record);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(
            filter,
            pageArgs,
            new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending),
            cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var page = await _store.FindManyAsync(q => ApplyFilter(q, filter), pageArgs, order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return await MapAsync(page);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return (await MapAsync(records)).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return (await MapAsync(records)).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<long> CountAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(query => ApplyFilter(query, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await SummarizeManyAsync(
            filter,
            pageArgs,
            new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending),
            cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _store.FindManyAsync<WorkflowInstanceSummary>(q => ApplyFilter(q, filter), pageArgs, order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await SummarizeManyAsync(filter, new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrder>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrder> order, CancellationToken cancellationToken = default)
    {
        return await _store.FindManyAsync<WorkflowInstanceSummary>(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        var record = await MapAsync(instance);
        await _store.SaveAsync(record, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default)
    {
        var records = await MapAsync(instances);
        await _store.SaveManyAsync(records, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    private void ApplyFilter(ParameterizedQuery query, WorkflowInstanceFilter filter)
    {
        query
            .Is(nameof(WorkflowInstance.Id), filter.Id)
            .In(nameof(WorkflowInstance.Id), filter.Ids)
            .Is(nameof(WorkflowInstance.DefinitionId), filter.DefinitionId)
            .In(nameof(WorkflowInstance.DefinitionId), filter.DefinitionIds)
            .Is(nameof(WorkflowInstance.DefinitionVersionId), filter.DefinitionVersionId)
            .In(nameof(WorkflowInstance.DefinitionVersionId), filter.DefinitionVersionIds)
            .Is(nameof(WorkflowInstance.Status), filter.WorkflowStatus?.ToString())
            .Is(nameof(WorkflowInstance.SubStatus), filter.WorkflowSubStatus?.ToString())
            .Is(nameof(WorkflowInstance.Name), filter.Version)
            .Is(nameof(WorkflowInstance.CorrelationId), filter.CorrelationId)
            .In(nameof(WorkflowInstance.CorrelationId), filter.CorrelationIds)
            .In(nameof(WorkflowInstance.Status), filter.WorkflowStatuses?.Select(x => x.ToString()))
            .In(nameof(WorkflowInstance.SubStatus), filter.WorkflowSubStatuses?.Select(x => x.ToString()))
            .AndWorkflowInstanceSearchTerm(filter.SearchTerm);
    }

    private async ValueTask<Page<WorkflowInstance>> MapAsync(Page<WorkflowInstanceRecord> source)
    {
        var items = (await MapAsync(source.Items)).ToList();
        return Page.Of(items, source.TotalCount);
    }

    private async ValueTask<IEnumerable<WorkflowInstance>> MapAsync(IEnumerable<WorkflowInstanceRecord> source) =>
        await Task.WhenAll(source.Select(async x => await MapAsync(x)));

    private async ValueTask<IEnumerable<WorkflowInstanceRecord>> MapAsync(IEnumerable<WorkflowInstance> source) =>
        await Task.WhenAll(source.Select(async x => await MapAsync(x)));

    private async ValueTask<WorkflowInstance> MapAsync(WorkflowInstanceRecord source)
    {
        var workflowState = await _workflowStateSerializer.DeserializeAsync(source.WorkflowState);
        return new WorkflowInstance
        {
            Id = source.Id,
            DefinitionId = source.DefinitionId,
            DefinitionVersionId = source.DefinitionVersionId,
            Version = source.Version,
            Name = source.Name,
            IncidentCount = source.IncidentCount,
            WorkflowState = workflowState,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            FinishedAt = source.FinishedAt,
            Status = Enum.Parse<WorkflowStatus>(source.Status),
            SubStatus = Enum.Parse<WorkflowSubStatus>(source.SubStatus),
            CorrelationId = source.CorrelationId,
        };
    }

    private async ValueTask<WorkflowInstanceRecord> MapAsync(WorkflowInstance source)
    {
        var workflowState = await _workflowStateSerializer.SerializeAsync(source.WorkflowState);
        return new WorkflowInstanceRecord
        {
            Id = source.Id,
            DefinitionId = source.DefinitionId,
            DefinitionVersionId = source.DefinitionVersionId,
            Version = source.Version,
            Name = source.Name,
            IncidentCount = source.IncidentCount,
            WorkflowState = workflowState,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            FinishedAt = source.FinishedAt,
            Status = source.Status.ToString(),
            SubStatus = source.SubStatus.ToString(),
            CorrelationId = source.CorrelationId
        };
    }
}