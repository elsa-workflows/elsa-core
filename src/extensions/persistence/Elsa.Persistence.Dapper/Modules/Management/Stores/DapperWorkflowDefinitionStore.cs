using System.Data;
using System.Data.Common;
using System.Text.Json;
using Dapper;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Models;
using Elsa.Persistence.Dapper.Modules.Management.Records;
using Elsa.Persistence.Dapper.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Npgsql;

namespace Elsa.Persistence.Dapper.Modules.Management.Stores;

/// <summary>
/// Provides a Dapper implementation of <see cref="IWorkflowDefinitionStore"/>.
/// </summary>
[UsedImplicitly]
internal class DapperWorkflowDefinitionStore(Store<WorkflowDefinitionRecord> store, IPayloadSerializer payloadSerializer, IDbConnectionProvider connectionProvider, ITenantAccessor? tenantAccessor = null)
    : IWorkflowDefinitionStore
{
    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record == null ? null : Map(record);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return record == null ? null : Map(record);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(
            filter,
            new WorkflowDefinitionOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending),
            pageArgs,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var page = await store.FindManyAsync(q => ApplyFilter(q, filter), pageArgs, order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return Map(page);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return Map(records).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return Map(records).ToList();
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await FindSummariesAsync(
            filter,
            new WorkflowDefinitionOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending),
            pageArgs,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await store.FindManyAsync<WorkflowDefinitionSummary>(q => ApplyFilter(q, filter), pageArgs, order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync<WorkflowDefinitionSummary>(q => ApplyFilter(q, filter), cancellationToken);
        return records.ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await store.FindManyAsync<WorkflowDefinitionSummary>(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), nameof(WorkflowDefinitionRecord.Version), OrderDirection.Descending, cancellationToken);
        return record == null ? null : Map(record);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var record = Map(definition);
        await store.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinitionUpdateResult> TryUpdateLatestAsync(
        WorkflowDefinitionFilter filter,
        Func<WorkflowDefinition, bool> matchesExpected,
        Func<WorkflowDefinition, WorkflowDefinition> update,
        CancellationToken cancellationToken = default)
    {
        // Keep the read and both possible writes in the same serializable transaction.
        // This also protects against existing SaveAsync writers, which do not carry a CAS stamp.
        using var connection = connectionProvider.GetConnection();
        if (connection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }
        else
        {
            cancellationToken.ThrowIfCancellationRequested();
            connection.Open();
        }

        var invokingCallback = false;
        try
        {
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            var record = await FindInScopeAsync(latestOnly: true);
            if (record == null)
            {
                // A filter can name a version that has since been superseded (for example by Id). As in the
                // memory, EF Core and MongoDB stores, that lost race is a Conflict; only a missing row is NotFound.
                return await FindInScopeAsync(latestOnly: false) == null
                    ? WorkflowDefinitionUpdateResult.NotFound()
                    : WorkflowDefinitionUpdateResult.Conflict();
            }

            var current = Map(record);
            invokingCallback = true;
            if (!matchesExpected(current))
            {
                return WorkflowDefinitionUpdateResult.Conflict();
            }

            var next = update(current);
            invokingCallback = false;
            if (next.TenantId != record.TenantId || next.DefinitionId != record.DefinitionId)
            {
                throw new InvalidOperationException("An atomic workflow update cannot change its tenant or logical definition.");
            }

            var nextRecord = Map(next);
            // ToolVersion is not part of the public entity. Preserve it on both update and draft creation.
            nextRecord.ToolVersion = record.ToolVersion;
            var write = connectionProvider.CreateQuery();
            if (next.Id == record.Id)
            {
                write.Update(store.TableName, nextRecord, store.PrimaryKey);
            }
            else
            {
                record.IsLatest = false;
                write.Update(store.TableName, record, store.PrimaryKey, [nameof(WorkflowDefinitionRecord.IsLatest)]);
            }

            var changed = await connection.ExecuteAsync(new CommandDefinition(
                write.Sql.ToString(), write.Parameters, transaction, cancellationToken: cancellationToken));
            if (changed != 1)
            {
                return WorkflowDefinitionUpdateResult.Conflict();
            }

            if (next.Id != record.Id)
            {
                var insert = connectionProvider.CreateQuery().Insert(store.TableName, nextRecord);
                // Explicitly type nullable binary data, just as the existing update path does.
                insert.Parameters.Add(nameof(WorkflowDefinitionRecord.BinaryData), nextRecord.BinaryData, DbType.Binary);
                await connection.ExecuteAsync(new CommandDefinition(
                    insert.Sql.ToString(), insert.Parameters, transaction, cancellationToken: cancellationToken));
            }

            cancellationToken.ThrowIfCancellationRequested();
            transaction.Commit();
            return WorkflowDefinitionUpdateResult.Updated(next);

            Task<WorkflowDefinitionRecord?> FindInScopeAsync(bool latestOnly)
            {
                var query = connectionProvider.CreateQuery().From(store.TableName);
                ApplyFilter(query, filter);
                if (latestOnly)
                {
                    query.Is(nameof(WorkflowDefinitionRecord.IsLatest), true);
                }
                if (!filter.TenantAgnostic)
                {
                    query.Is(nameof(WorkflowDefinitionRecord.TenantId), (object?)tenantAccessor?.Tenant?.Id ?? DBNull.Value);
                }

                return connection.QueryFirstOrDefaultAsync<WorkflowDefinitionRecord?>(new CommandDefinition(
                    query.Sql.ToString(), query.Parameters, transaction, cancellationToken: cancellationToken));
            }
        }
        catch (DbException exception) when (!invokingCallback && IsAbortedContention(exception))
        {
            // Only known aborted contention is a conflict. Transport/ambiguous commit failures propagate.
            return WorkflowDefinitionUpdateResult.Conflict();
        }
    }

    private static bool IsAbortedContention(DbException exception) => exception switch
    {
        SqliteException { SqliteErrorCode: 5 or 6 } => true,
        SqlException { Number: 1205 } => true,
        PostgresException { SqlState: "40001" or "40P01" } => true,
        _ => false
    };

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
    {
        var records = definitions.Select(Map).ToList();
        await store.SaveManyAsync(records, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.AnyAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountDistinctAsync(CancellationToken cancellationToken = default)
    {
        return await store.CountAsync(
            filter => filter.Count($"distinct {nameof(WorkflowDefinition.DefinitionId)}", store.TableName),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> GetIsNameUnique(string name, string? definitionId = null, CancellationToken cancellationToken = default)
    {
        var exists = await store.AnyAsync(query =>
        {
            query.Is(nameof(WorkflowDefinition.Name), name);

            if (definitionId != null)
                query.IsNot(nameof(WorkflowDefinition.DefinitionId), definitionId);
        }, cancellationToken);

        return !exists;
    }

    private void ApplyFilter(ParameterizedQuery query, WorkflowDefinitionFilter filter)
    {
        var definitionId = filter.DefinitionId ?? filter.DefinitionHandle?.DefinitionId;
        var versionOptions = filter.VersionOptions ?? filter.DefinitionHandle?.VersionOptions;
        var id = filter.Id ?? filter.DefinitionHandle?.DefinitionVersionId;
        
        query
            .Is(nameof(WorkflowDefinition.DefinitionId), definitionId)
            .In(nameof(WorkflowDefinition.DefinitionId), filter.DefinitionIds)
            .Is(nameof(WorkflowDefinition.Id), id)
            .In(nameof(WorkflowDefinition.Id), filter.Ids)
            .Is(versionOptions)
            .Is(nameof(WorkflowDefinition.MaterializerName), filter.MaterializerName)
            .Is(nameof(WorkflowDefinition.Name), filter.Name)
            .In(nameof(WorkflowDefinition.Name), filter.Names)
            .Is(nameof(WorkflowDefinition.Options.UsableAsActivity), filter.UsableAsActivity)
            .Is(nameof(WorkflowDefinition.IsSystem), filter.IsSystem)
            .Is(nameof(WorkflowDefinition.IsReadonly), filter.IsReadonly)
            .WorkflowDefinitionSearchTerm(filter.SearchTerm);
    }

    private Page<WorkflowDefinition> Map(Page<WorkflowDefinitionRecord> source) => new(Map(source.Items).ToList(), source.TotalCount);
    private IEnumerable<WorkflowDefinition> Map(IEnumerable<WorkflowDefinitionRecord> records) => records.Select(Map);

    private WorkflowDefinition Map(WorkflowDefinitionRecord source)
    {
        var props = payloadSerializer.Deserialize<WorkflowDefinitionProps>(source.Props);
        return new WorkflowDefinition
        {
            Id = source.Id,
            DefinitionId = source.DefinitionId,
            Version = source.Version,
            Name = source.Name,
            Description = source.Description,
            IsPublished = source.IsPublished,
            IsLatest = source.IsLatest,
            IsReadonly = source.IsReadonly,
            IsSystem = source.IsSystem,
            CreatedAt = source.CreatedAt,
            StringData = source.StringData,
            Options = props.Options,
            Variables = props.Variables,
            Inputs = props.Inputs,
            Outputs = props.Outputs,
            Outcomes = props.Outcomes,
            CustomProperties = props.CustomProperties,
            MaterializerContext = source.MaterializerContext,
            BinaryData = source.BinaryData,
            MaterializerName = source.MaterializerName,
            ProviderName = source.ProviderName,
            TenantId = source.TenantId
        };
    }

    private WorkflowDefinitionRecord Map(WorkflowDefinition source)
    {
        var props = new WorkflowDefinitionProps
        {
            Options = source.Options,
            Variables = source.Variables,
            Inputs = source.Inputs,
            Outputs = source.Outputs,
            Outcomes = source.Outcomes,
            CustomProperties = source.CustomProperties
        };

        return new WorkflowDefinitionRecord
        {
            Id = source.Id,
            DefinitionId = source.DefinitionId,
            Version = source.Version,
            Name = source.Name,
            Description = source.Description,
            IsPublished = source.IsPublished,
            IsLatest = source.IsLatest,
            CreatedAt = source.CreatedAt,
            IsReadonly = source.IsReadonly,
            IsSystem = source.IsSystem,
            StringData = source.StringData,
            Props = payloadSerializer.Serialize(props),
            MaterializerName = source.MaterializerName,
            UsableAsActivity = source.Options.UsableAsActivity,
            ProviderName = source.ProviderName,
            BinaryData = source.BinaryData,
            MaterializerContext = source.MaterializerContext,
            TenantId = source.TenantId
        };
    }

    private class WorkflowDefinitionProps
    {
        [JsonConstructor]
        public WorkflowDefinitionProps()
        {
        }

        public WorkflowDefinitionProps(
            WorkflowOptions options,
            ICollection<Variable> variables,
            ICollection<InputDefinition> inputs,
            ICollection<OutputDefinition> outputs,
            ICollection<string> outcomes,
            IDictionary<string, object> customProperties
        )
        {
            Options = options;
            Variables = variables;
            Inputs = inputs;
            Outputs = outputs;
            Outcomes = outcomes;
            CustomProperties = customProperties;
        }

        public WorkflowOptions Options { get; set; } = new();
        public ICollection<Variable> Variables { get; set; } = new List<Variable>();
        public ICollection<InputDefinition> Inputs { get; set; } = new List<InputDefinition>();
        public ICollection<OutputDefinition> Outputs { get; set; } = new List<OutputDefinition>();
        public ICollection<string> Outcomes { get; set; } = new List<string>();
        public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    }
}
