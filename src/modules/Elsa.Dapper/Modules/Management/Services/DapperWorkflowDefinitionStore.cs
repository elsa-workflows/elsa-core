using System.Text.Json.Serialization;
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
/// Provides a Dapper implementation of <see cref="IWorkflowDefinitionStore"/>.
/// </summary>
public class DapperWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private const string TableName = "WorkflowDefinitions";
    private readonly IDbConnectionProvider _dbConnectionProvider;
    private readonly IPayloadSerializer _payloadSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperWorkflowDefinitionStore"/> class.
    /// </summary>
    public DapperWorkflowDefinitionStore(IDbConnectionProvider dbConnectionProvider, IPayloadSerializer payloadSerializer)
    {
        _dbConnectionProvider = dbConnectionProvider;
        _payloadSerializer = payloadSerializer;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery(filter);
        var record = await query.SingleOrDefaultAsync<WorkflowDefinitionRecord>(connection);
        return record == null ? null : Map(record);
    }


    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery(filter).OrderBy(order.KeySelector, order.Direction);
        var record = await query.SingleOrDefaultAsync<WorkflowDefinitionRecord>(connection);
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
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery(filter).OrderBy(order.KeySelector, order.Direction).Page(pageArgs);
        var countQuery = CreateCountQuery(filter);
        var records = await query.QueryAsync<WorkflowDefinitionRecord>(connection);
        var totalCount = await countQuery.SingleAsync<long>(connection);
        var entities = Map(records).ToList();
        return Page.Of(entities, totalCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery(filter);
        var records = await query.QueryAsync<WorkflowDefinitionRecord>(connection);
        return Map(records).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery(filter).OrderBy(order.KeySelector, order.Direction);
        var records = await query.QueryAsync<WorkflowDefinitionRecord>(connection);
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
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery(filter).OrderBy(order.KeySelector, order.Direction).Page(pageArgs);
        var countQuery = CreateCountQuery(filter);
        var records = (await query.QueryAsync<WorkflowDefinitionSummary>(connection)).ToList();
        var totalCount = await countQuery.SingleAsync<long>(connection);
        return Page.Of(records, totalCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery(filter);
        var records = await query.QueryAsync<WorkflowDefinitionSummary>(connection);
        return records.ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery(filter).OrderBy(order.KeySelector, order.Direction);
        var records = await query.QueryAsync<WorkflowDefinitionSummary>(connection);
        return records.ToList();
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var query = CreateSelectQuery(filter).OrderBy<WorkflowDefinitionRecord, int>(x => x.Version, OrderDirection.Descending);
        var record = await query.SingleOrDefaultAsync<WorkflowDefinitionRecord>(connection);
        return record == null ? null : Map(record);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var record = Map(definition);
        using var connection = _dbConnectionProvider.GetConnection();
        var query = new ParameterizedQuery(_dbConnectionProvider.Dialect).Upsert(TableName, nameof(WorkflowDefinitionRecord.Id), record);
        await query.SingleAsync<int>(connection);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
    {
        foreach (var definition in definitions) 
            await SaveAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var query = CreateDeleteQuery(filter);
        using var connection = _dbConnectionProvider.GetConnection();
        return await query.ExecuteAsync(connection);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionProvider.GetConnection();
        var countQuery = CreateCountQuery(filter);
        var totalCount = await countQuery.SingleAsync<long>(connection);
        return totalCount > 0;
    }

    private ParameterizedQuery CreateCountQuery(WorkflowDefinitionFilter filter)
    {
        var query = new ParameterizedQuery(_dbConnectionProvider.Dialect).Count(TableName);
        return ApplyQueryClauses(query, filter);
    }

    private ParameterizedQuery CreateSelectQuery(WorkflowDefinitionFilter filter)
    {
        var query = new ParameterizedQuery(_dbConnectionProvider.Dialect).From(TableName);
        return ApplyQueryClauses(query, filter);
    }
    
    private ParameterizedQuery CreateDeleteQuery(WorkflowDefinitionFilter filter)
    {
        var query = new ParameterizedQuery(_dbConnectionProvider.Dialect).Delete(TableName);
        return ApplyQueryClauses(query, filter);
    }

    private ParameterizedQuery ApplyQueryClauses(ParameterizedQuery query, WorkflowDefinitionFilter filter)
    {
        return query
            .And(nameof(WorkflowDefinition.DefinitionId), filter.DefinitionId)
            .And(nameof(WorkflowDefinition.DefinitionId), filter.DefinitionIds)
            .And(nameof(WorkflowDefinition.Id), filter.Id)
            .And(nameof(WorkflowDefinition.Id), filter.Ids)
            .And(filter.VersionOptions)
            .And(nameof(WorkflowDefinition.MaterializerName), filter.MaterializerName)
            .And(nameof(WorkflowDefinition.Name), filter.Name)
            .And(nameof(WorkflowDefinition.Name), filter.Names)
            .And(nameof(WorkflowDefinition.Options.UsableAsActivity), filter.UsableAsActivity);
    }

    private IEnumerable<WorkflowDefinition> Map(IEnumerable<WorkflowDefinitionRecord> records)
    {
        return records.Select(Map);
    }

    private WorkflowDefinition Map(WorkflowDefinitionRecord record)
    {
        var props = _payloadSerializer.Deserialize<WorkflowDefinitionProps>(record.Props);
        var definition = new WorkflowDefinition
        {
            Id = record.Id,
            DefinitionId = record.DefinitionId,
            Version = record.Version,
            Name = record.Name,
            Description = record.Description,
            IsPublished = record.IsPublished,
            IsLatest = record.IsLatest,
            CreatedAt = record.CreatedAt,
            StringData = record.StringData,
            Options = props.Options,
            Variables = props.Variables,
            Inputs = props.Inputs,
            Outputs = props.Outputs,
            Outcomes = props.Outcomes,
            CustomProperties = props.CustomProperties
        };

        return definition;
    }

    private WorkflowDefinitionRecord Map(WorkflowDefinition workflowDefinition)
    {
        var props = new WorkflowDefinitionProps
        {
            Options = workflowDefinition.Options,
            Variables = workflowDefinition.Variables,
            Inputs = workflowDefinition.Inputs,
            Outputs = workflowDefinition.Outputs,
            Outcomes = workflowDefinition.Outcomes,
            CustomProperties = workflowDefinition.CustomProperties
        };

        var record = new WorkflowDefinitionRecord
        {
            Id = workflowDefinition.Id,
            DefinitionId = workflowDefinition.DefinitionId,
            Version = workflowDefinition.Version,
            Name = workflowDefinition.Name,
            Description = workflowDefinition.Description,
            IsPublished = workflowDefinition.IsPublished,
            IsLatest = workflowDefinition.IsLatest,
            CreatedAt = workflowDefinition.CreatedAt,
            StringData = workflowDefinition.StringData,
            Props = _payloadSerializer.Serialize(props)
        };

        return record;
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