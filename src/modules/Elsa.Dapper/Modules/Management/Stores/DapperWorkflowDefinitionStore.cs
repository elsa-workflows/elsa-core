using System.Text.Json.Serialization;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Management.Records;
using Elsa.Dapper.Services;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Dapper.Modules.Management.Stores;

/// <summary>
/// Provides a Dapper implementation of <see cref="IWorkflowDefinitionStore"/>.
/// </summary>
public class DapperWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private const string TableName = "WorkflowDefinitions";
    private const string PrimaryKeyName = "Id";
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly Store<WorkflowDefinitionRecord> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperWorkflowDefinitionStore"/> class.
    /// </summary>
    public DapperWorkflowDefinitionStore(IDbConnectionProvider dbConnectionProvider, IPayloadSerializer payloadSerializer)
    {
        _payloadSerializer = payloadSerializer;
        _store = new Store<WorkflowDefinitionRecord>(dbConnectionProvider, TableName, PrimaryKeyName);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await _store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record == null ? null : Map(record);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var record = await _store.FindAsync(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
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
        var page = await _store.FindManyAsync(q => ApplyFilter(q, filter), pageArgs, order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return Map(page);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return Map(records).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
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
        return await _store.FindManyAsync<WorkflowDefinitionSummary>(q => ApplyFilter(q, filter), pageArgs, order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync<WorkflowDefinitionSummary>(q => ApplyFilter(q, filter), cancellationToken);
        return records.ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _store.FindManyAsync<WorkflowDefinitionSummary>(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        var record = await _store.FindAsync(q => ApplyFilter(q, filter), nameof(WorkflowDefinitionRecord.Version), OrderDirection.Descending, cancellationToken);
        return record == null ? null : Map(record);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var record = Map(definition);
        await _store.SaveAsync(record, nameof(WorkflowDefinitionRecord.Id), cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
    {
        var records = definitions.Select(Map).ToList();
        await _store.SaveManyAsync(records, nameof(WorkflowDefinitionRecord.Id), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.AnyAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountDistinctAsync(CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(
            filter => filter.Count($"distinct {nameof(WorkflowDefinition.DefinitionId)}", TableName),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> GetIsNameUnique(string name, string? definitionId = default, CancellationToken cancellationToken = default)
    {
        var exists = await _store.AnyAsync(query =>
        {
            query.Is(nameof(WorkflowDefinition.Name), name);

            if (definitionId != null)
                query.IsNot(nameof(WorkflowDefinition.DefinitionId), definitionId);
        }, cancellationToken);

        return !exists;
    }

    private void ApplyFilter(ParameterizedQuery query, WorkflowDefinitionFilter filter)
    {
        query
            .Is(nameof(WorkflowDefinition.DefinitionId), filter.DefinitionId)
            .In(nameof(WorkflowDefinition.DefinitionId), filter.DefinitionIds)
            .Is(nameof(WorkflowDefinition.Id), filter.Id)
            .In(nameof(WorkflowDefinition.Id), filter.Ids)
            .Is(filter.VersionOptions)
            .Is(nameof(WorkflowDefinition.MaterializerName), filter.MaterializerName)
            .Is(nameof(WorkflowDefinition.Name), filter.Name)
            .In(nameof(WorkflowDefinition.Name), filter.Names)
            .Is(nameof(WorkflowDefinition.Options.UsableAsActivity), filter.UsableAsActivity)
            .WorkflowDefinitionSearchTerm(filter.SearchTerm);
    }

    private Page<WorkflowDefinition> Map(Page<WorkflowDefinitionRecord> source) => new(Map(source.Items).ToList(), source.TotalCount);
    private IEnumerable<WorkflowDefinition> Map(IEnumerable<WorkflowDefinitionRecord> records) => records.Select(Map);

    private WorkflowDefinition Map(WorkflowDefinitionRecord source)
    {
        var props = _payloadSerializer.Deserialize<WorkflowDefinitionProps>(source.Props);
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
            ProviderName = source.ProviderName
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
            StringData = source.StringData,
            Props = _payloadSerializer.Serialize(props),
            MaterializerName = source.MaterializerName,
            UsableAsActivity = source.Options.UsableAsActivity,
            ProviderName = source.ProviderName,
            BinaryData = source.BinaryData,
            MaterializerContext = source.MaterializerContext
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