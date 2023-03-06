using System.Text.Json.Serialization;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.Serialization;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <inheritdoc />
public class EFCoreWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly IDbContextFactory<RuntimeElsaDbContext> _dbContextFactory;
    private readonly EntityStore<RuntimeElsaDbContext, WorkflowExecutionLogRecord> _store;
    
    /// <summary>
    /// Constructor
    /// </summary>

    public EFCoreWorkflowExecutionLogStore(
        EntityStore<RuntimeElsaDbContext, WorkflowExecutionLogRecord> store, 
        IDbContextFactory<RuntimeElsaDbContext> dbContextFactory,
        SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
        _dbContextFactory = dbContextFactory;
    }
    
    /// <inheritdoc />
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        await _store.SaveManyAsync(records, Save, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyByWorkflowInstanceIdAsync(string workflowInstanceId, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(
            x => x.WorkflowInstanceId == workflowInstanceId,
            x => x.Timestamp,
            OrderDirection.Ascending,
            pageArgs,
            Load,
            cancellationToken);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        foreach (var record in records.Items)
        {
            var entry = dbContext.Entry(record);
            var json = entry.Property<string>("PayloadData").CurrentValue;
            if (!string.IsNullOrEmpty(json))
            {
                record.Payload = JsonSerializer.Deserialize<object>(json);
            }
        }

        return records;
    }
    
    private WorkflowExecutionLogRecord Save(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord entity)
    {
        var options = _serializerOptionsProvider.CreatePersistenceOptions(ReferenceHandler.Preserve);
        var json = JsonSerializer.Serialize(entity.Payload, options);

        dbContext.Entry(entity).Property("PayloadData").CurrentValue = json;
        return entity;
    }
    
    private WorkflowExecutionLogRecord? Load(RuntimeElsaDbContext dbContext, WorkflowExecutionLogRecord? entity)
    {
        if (entity is not null)
        {
            var json = dbContext.Entry(entity).Property<string>("PayloadData").CurrentValue;
            if (!string.IsNullOrEmpty(json))
            {
                entity.Payload = JsonSerializer.Deserialize<object>(json);
            }
        }
        return entity;
    }
}