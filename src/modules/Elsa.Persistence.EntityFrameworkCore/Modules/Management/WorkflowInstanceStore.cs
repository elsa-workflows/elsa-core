using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Management;

public class EFCoreWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly Store<ManagementDbContext, WorkflowInstance> _store;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public EFCoreWorkflowInstanceStore(Store<ManagementDbContext, WorkflowInstance> store, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    public async Task<WorkflowInstance?> FindByIdAsync(string id, CancellationToken cancellationToken = default) =>
        await _store.FindAsync(x => x.Id == id, Load, cancellationToken);

    public async Task SaveAsync(WorkflowInstance record, CancellationToken cancellationToken = default) =>
        await _store.SaveAsync(record, Save, cancellationToken);

    public async Task SaveManyAsync(IEnumerable<WorkflowInstance> records, CancellationToken cancellationToken = default) =>
        await _store.SaveManyAsync(records, Save, cancellationToken);

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        await _store.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;

    public async Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _store.DeleteWhereAsync(x => idList.Contains(x.Id), cancellationToken);
    }

    public async Task DeleteManyByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) =>
        await _store.DeleteWhereAsync(x => x.DefinitionId == definitionId, cancellationToken);

    public async Task<Page<WorkflowInstanceSummary>> FindManyAsync(FindWorkflowInstancesArgs args, CancellationToken cancellationToken = default)
    {
        var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var query = dbContext.WorkflowInstances.AsQueryable();
        var (searchTerm, definitionId, version, correlationId, workflowStatus, workflowSubStatus, pageArgs, orderBy, orderDirection) = args;

        if (!string.IsNullOrWhiteSpace(definitionId))
            query = query.Where(x => x.DefinitionId == definitionId);

        if (version != null)
            query = query.Where(x => x.Version == version);

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(x => x.CorrelationId == correlationId);

        if (workflowStatus != null)
            query = query.Where(x => x.Status == workflowStatus);

        if (workflowSubStatus != null)
            query = query.Where(x => x.SubStatus == workflowSubStatus);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query =
                from instance in query
                where instance.Name!.Contains(searchTerm)
                      || instance.Id.Contains(searchTerm)
                      || instance.DefinitionId.Contains(searchTerm)
                      || instance.CorrelationId!.Contains(searchTerm)
                select instance;
        }

        query = orderBy switch
        {
            OrderBy.Finished => orderDirection == OrderDirection.Ascending ? query.OrderBy(x => x.FinishedAt) : query.OrderByDescending(x => x.FinishedAt),
            OrderBy.LastExecuted => orderDirection == OrderDirection.Ascending ? query.OrderBy(x => x.LastExecutedAt) : query.OrderByDescending(x => x.LastExecutedAt),
            OrderBy.Created => orderDirection == OrderDirection.Ascending ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt),
            _ => query
        };

        return await query.PaginateAsync(x => WorkflowInstanceSummary.FromInstance(Load(dbContext, x)!), pageArgs);
    }

    public WorkflowInstance Save(ManagementDbContext dbContext, WorkflowInstance entity)
    {
        var data = new WorkflowInstanceState(entity.WorkflowState, entity.Fault);
        var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
        var json = JsonSerializer.Serialize(data, options);

        dbContext.Entry(entity).Property("Data").CurrentValue = json;
        return entity;
    }

    public WorkflowInstance? Load(ManagementDbContext dbContext, WorkflowInstance? entity)
    {
        if (entity == null)
            return null;

        var data = new WorkflowInstanceState(entity.WorkflowState, entity.Fault);
        var json = (string?)dbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
            data = JsonSerializer.Deserialize<WorkflowInstanceState>(json, options)!;
        }

        entity.WorkflowState = data.WorkflowState;
        entity.Fault = data.Fault;

        return entity;
    }

    private class WorkflowInstanceState
    {
        public WorkflowInstanceState()
        {
        }

        public WorkflowInstanceState(WorkflowState workflowState, WorkflowFault? fault)
        {
            WorkflowState = workflowState;
            Fault = fault;
        }

        public WorkflowState WorkflowState { get; init; } = default!;
        public WorkflowFault? Fault { get; set; }
    }
}