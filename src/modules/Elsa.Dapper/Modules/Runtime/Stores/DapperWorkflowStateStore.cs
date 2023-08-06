using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Dapper.Modules.Runtime.Stores;

/// <summary>
/// Provides a Dapper implementation of <see cref="IWorkflowStateStore"/>.
/// </summary>
public class DapperWorkflowStateStore : IWorkflowStateStore
{
    private const string TableName = "WorkflowStates";
    private const string PrimaryKeyName = "Id";
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly Store<WorkflowStateRecord> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperWorkflowStateStore"/> class.
    /// </summary>
    public DapperWorkflowStateStore(IDbConnectionProvider dbConnectionProvider, IWorkflowStateSerializer workflowStateSerializer)
    {
        _workflowStateSerializer = workflowStateSerializer;
        _store = new Store<WorkflowStateRecord>(dbConnectionProvider, TableName, PrimaryKeyName);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(string id, WorkflowState state, CancellationToken cancellationToken = default)
    {
        var record = await MapAsync(state, cancellationToken);
        await _store.SaveAsync(record, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<WorkflowState?> FindAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await _store.FindAsync(query => ApplyFilter(query, filter), cancellationToken);
        return record == null ? null : await MapAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> CountAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(query => ApplyFilter(query, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    private async Task<WorkflowStateRecord> MapAsync(WorkflowState source, CancellationToken cancellationToken)
    {
        return new WorkflowStateRecord
        {
            Id = source.Id,
            DefinitionId = source.DefinitionId,
            DefinitionVersion = source.DefinitionVersion,
            CorrelationId = source.CorrelationId,
            Status = source.Status.ToString(),
            SubStatus = source.SubStatus.ToString(),
            Props = await _workflowStateSerializer.SerializeAsync(source, cancellationToken)
        };
    }

    private async Task<WorkflowState> MapAsync(WorkflowStateRecord source, CancellationToken cancellationToken)
    {
        return await _workflowStateSerializer.DeserializeAsync(source.Props, cancellationToken);
    }
    
    private void ApplyFilter(ParameterizedQuery query, WorkflowStateFilter filter)
    {
        query
            .Is(nameof(WorkflowState.Id), filter.Id)
            .In(nameof(WorkflowState.Id), filter.Ids)
            .Is(nameof(WorkflowState.CorrelationId), filter.CorrelationId)
            .In(nameof(WorkflowState.CorrelationId), filter.CorrelationIds)
            .Is(nameof(WorkflowState.DefinitionId), filter.DefinitionId)
            .In(nameof(WorkflowState.DefinitionId), filter.DefinitionIds)
            .Is(nameof(WorkflowState.DefinitionVersion), filter.Version)
            .Is(nameof(WorkflowState.DefinitionVersionId), filter.DefinitionVersionId)
            .In(nameof(WorkflowState.DefinitionVersionId), filter.DefinitionVersionIds)
            .Is(nameof(WorkflowState.Status), filter.WorkflowStatus?.ToString())
            .Is(nameof(WorkflowState.SubStatus), filter.WorkflowSubStatus?.ToString())
            .In(nameof(WorkflowState.Status), filter.WorkflowStatuses?.Select(x => x.ToString()))
            .In(nameof(WorkflowState.SubStatus), filter.WorkflowSubStatuses?.Select(x => x.ToString()))
            ;
    }
}