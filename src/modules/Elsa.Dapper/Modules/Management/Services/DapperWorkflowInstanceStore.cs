using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Management.Records;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Dapper.Modules.Management.Services;

/// <summary>
/// Provides a Dapper implementation of <see cref="IWorkflowInstanceStore"/>.
/// </summary>
public class DapperWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly IDbConnectionProvider _dbConnectionProvider;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private const string TableName = "WorkflowInstances";

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperWorkflowInstanceStore"/> class.
    /// </summary>
    public DapperWorkflowInstanceStore(IDbConnectionProvider dbConnectionProvider, IWorkflowStateSerializer workflowStateSerializer)
    {
        _dbConnectionProvider = dbConnectionProvider;
        _workflowStateSerializer = workflowStateSerializer;
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery<WorkflowInstanceRecord>(filter);
        var record = await query.SingleOrDefaultAsync<WorkflowInstanceRecord>(connection);
        return record == null ? null : await MapAsync(record);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(
            filter,
            pageArgs,
            new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery<WorkflowInstanceRecord>(filter).OrderBy(order.KeySelector, order.Direction).Page(pageArgs);
        var countQuery = CreateCountQuery(filter);
        var records = await query.QueryAsync<WorkflowInstanceRecord>(connection);
        var totalCount = await countQuery.SingleAsync<long>(connection);
        var entities = (await MapAsync(records)).ToList();
        return Page.Of(entities, totalCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery<WorkflowInstanceRecord>(filter);
        var records = await query.QueryAsync<WorkflowInstanceRecord>(connection);
        return (await MapAsync(records)).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery<WorkflowInstanceRecord>(filter).OrderBy(order.KeySelector, order.Direction);
        var records = await query.QueryAsync<WorkflowInstanceRecord>(connection);
        return (await MapAsync(records)).ToList();
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await SummarizeManyAsync(
            filter,
            pageArgs,
            new WorkflowInstanceOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery<WorkflowInstanceSummary>(filter).OrderBy(order.KeySelector, order.Direction).Page(pageArgs);
        var countQuery = CreateCountQuery(filter);
        var records = (await query.QueryAsync<WorkflowInstanceSummary>(connection)).ToList();
        var totalCount = await countQuery.SingleAsync<long>(connection);
        return Page.Of(records, totalCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery<WorkflowInstanceSummary>(filter);
        var records = await query.QueryAsync<WorkflowInstanceSummary>(connection);
        return records.ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrder>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrder> order, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery<WorkflowInstanceSummary>(filter).OrderBy(order.KeySelector, order.Direction);
        var records = await query.QueryAsync<WorkflowInstanceSummary>(connection);
        return records.ToList();
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        var record = await MapAsync(instance);
        using var connection = _dbConnectionProvider.GetConnection();
        var query = new ParameterizedQuery(_dbConnectionProvider.Dialect).Upsert(TableName, nameof(WorkflowDefinitionRecord.Id), record);
        await query.ExecuteAsync(connection);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default)
    {
        var query = new ParameterizedQuery(_dbConnectionProvider.Dialect);
        
        foreach (var instance in instances)
        {
            var record = await MapAsync(instance);
            query.Upsert(TableName, nameof(WorkflowDefinitionRecord.Id), record);
        }
        
        using var connection = _dbConnectionProvider.GetConnection();
        await query.ExecuteAsync(connection);
    }

    /// <inheritdoc />
    public async Task<int> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var query = CreateDeleteQuery(filter);
        using var connection = _dbConnectionProvider.GetConnection();
        return await query.ExecuteAsync(connection);
    }

    private ParameterizedQuery CreateCountQuery(WorkflowInstanceFilter filter)
    {
        var query = new ParameterizedQuery(_dbConnectionProvider.Dialect).Count(TableName);
        return ApplyQueryClauses(query, filter);
    }

    private ParameterizedQuery CreateSelectQuery<T>(WorkflowInstanceFilter filter)
    {
        var query = new ParameterizedQuery(_dbConnectionProvider.Dialect).From<T>(TableName);
        return ApplyQueryClauses(query, filter);
    }

    private ParameterizedQuery CreateDeleteQuery(WorkflowInstanceFilter filter)
    {
        var query = new ParameterizedQuery(_dbConnectionProvider.Dialect).Delete(TableName);
        return ApplyQueryClauses(query, filter);
    }

    private ParameterizedQuery ApplyQueryClauses(ParameterizedQuery query, WorkflowInstanceFilter filter)
    {
        return query
                .And(nameof(WorkflowInstance.Id), filter.Id)
                .And(nameof(WorkflowInstance.Id), filter.Ids)
                .And(nameof(WorkflowInstance.DefinitionId), filter.DefinitionId)
                .And(nameof(WorkflowInstance.DefinitionId), filter.DefinitionIds)
                .And(nameof(WorkflowInstance.DefinitionVersionId), filter.DefinitionVersionId)
                .And(nameof(WorkflowInstance.DefinitionVersionId), filter.DefinitionVersionIds)
                .And(nameof(WorkflowInstance.Status), filter.WorkflowStatus?.ToString())
                .And(nameof(WorkflowInstance.SubStatus), filter.WorkflowSubStatus?.ToString())
                .And(nameof(WorkflowInstance.Name), filter.Version)
                .And(nameof(WorkflowInstance.CorrelationId), filter.CorrelationId)
                .And(nameof(WorkflowInstance.CorrelationId), filter.CorrelationIds)
                .AndWorkflowInstanceSearchTerm(filter.SearchTerm)
            ;
    }

    private async Task<IEnumerable<WorkflowInstance>> MapAsync(IEnumerable<WorkflowInstanceRecord> source) =>
        await Task.WhenAll(source.Select(async x => await MapAsync(x)));

    private async Task<WorkflowInstance> MapAsync(WorkflowInstanceRecord source)
    {
        var workflowState = await _workflowStateSerializer.DeserializeAsync(source.WorkflowState);
        return new WorkflowInstance
        {
            Id = source.Id,
            DefinitionId = source.DefinitionId,
            DefinitionVersionId = source.DefinitionVersionId,
            Version = source.Version,
            Name = source.Name,
            WorkflowState = workflowState,
            CreatedAt = source.CreatedAt,
            LastExecutedAt = source.LastExecutedAt,
            FinishedAt = source.FinishedAt,
            FaultedAt = source.FaultedAt,
            CancelledAt = source.CancelledAt,
            Status = Enum.Parse<WorkflowStatus>(source.Status),
            SubStatus = Enum.Parse<WorkflowSubStatus>(source.SubStatus),
            CorrelationId = source.CorrelationId,
        };
    }

    private async Task<WorkflowInstanceRecord> MapAsync(WorkflowInstance source)
    {
        var workflowState = await _workflowStateSerializer.SerializeAsync(source.WorkflowState);
        return new WorkflowInstanceRecord
        {
            Id = source.Id,
            DefinitionId = source.DefinitionId,
            DefinitionVersionId = source.DefinitionVersionId,
            Version = source.Version,
            Name = source.Name,
            WorkflowState = workflowState,
            CreatedAt = source.CreatedAt,
            LastExecutedAt = source.LastExecutedAt,
            FinishedAt = source.FinishedAt,
            FaultedAt = source.FaultedAt,
            CancelledAt = source.CancelledAt,
            Status = source.Status.ToString(),
            SubStatus = source.SubStatus.ToString(),
            CorrelationId = source.CorrelationId
        };
    }
}