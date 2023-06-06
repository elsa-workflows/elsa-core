using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Dapper.Modules.Runtime.Services;

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
    public async ValueTask<WorkflowState?> LoadAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = await _store.FindAsync(q => q.Equals(PrimaryKeyName, id), cancellationToken);
        return record == null ? null : await MapAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> CountAsync(CountRunningWorkflowsArgs args, CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(q =>
        {
            q
                .Equals(nameof(WorkflowStateRecord.Status), WorkflowStatus.Running)
                .Equals(nameof(WorkflowStateRecord.DefinitionId), args.DefinitionId)
                .Equals(nameof(WorkflowStateRecord.CorrelationId), args.CorrelationId);
        }, cancellationToken);
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
}