using Elsa.Dapper.Contracts;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Dapper.Modules.Runtime.Services;

/// <summary>
/// Provides a Dapper implementation of <see cref="ITriggerStore"/>.
/// </summary>
public class DapperTriggerStore : ITriggerStore
{
    private const string TableName = "Triggers";
    private const string PrimaryKeyName = "Id";
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly Store<StoredTriggerRecord> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperTriggerStore"/> class.
    /// </summary>
    public DapperTriggerStore(IDbConnectionProvider dbConnectionProvider, IPayloadSerializer payloadSerializer)
    {
        _payloadSerializer = payloadSerializer;
        _store = new Store<StoredTriggerRecord>(dbConnectionProvider, TableName, PrimaryKeyName);
    }


    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = Map(record);
    }

    public ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private StoredTriggerRecord Map(StoredTrigger source)
    {
        return new StoredTriggerRecord
        {
            Id = source.Id,
            ActivityId = source.ActivityId,
            Hash = source.Hash,
            Name = source.Name,
            WorkflowDefinitionId = source.WorkflowDefinitionId,
            SerializedPayload = source.Payload != null ? _payloadSerializer.Serialize(source.Payload) : default
        };
    }
}