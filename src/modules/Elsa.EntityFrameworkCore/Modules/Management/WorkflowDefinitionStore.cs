using System.Text.Json.Serialization;
using Elsa.Common.Models;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Microsoft.EntityFrameworkCore;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Modules.Management;

/// <inheritdoc />
public class EFCoreWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly EntityStore<ManagementElsaDbContext, WorkflowDefinition> _store;
    private readonly IPayloadSerializer _payloadSerializer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreWorkflowDefinitionStore(EntityStore<ManagementElsaDbContext, WorkflowDefinition> store, IPayloadSerializer payloadSerializer)
    {
        _store = store;
        _payloadSerializer = payloadSerializer;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(queryable => Filter(queryable, filter), OnLoadAsync, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), OnLoadAsync, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(queryable => Filter(queryable, filter), cancellationToken).LongCount();
        var results = await _store.QueryAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), OnLoadAsync, cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), cancellationToken).LongCount();
        var results = await _store.QueryAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), OnLoadAsync, cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(queryable => Filter(queryable, filter), OnLoadAsync, cancellationToken).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), OnLoadAsync, cancellationToken).ToList();
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions.AsNoTracking();
        var queryable = Filter(set.AsQueryable(), filter);
        var count = await queryable.LongCountAsync(cancellationToken);
        queryable = Paginate(queryable, pageArgs);
        var results = await queryable.Select(WorkflowDefinitionSummary.FromDefinitionExpression()).ToListAsync(cancellationToken);

        return Page.Of(results, count);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions.AsNoTracking();

        var queryable = Filter(set.AsQueryable(), filter).OrderBy(order);

        var count = await queryable.LongCountAsync(cancellationToken);
        queryable = Paginate(queryable, pageArgs);
        var results = await queryable.Select(WorkflowDefinitionSummary.FromDefinitionExpression()).ToListAsync(cancellationToken);
        return Page.Of(results, count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions.AsNoTracking();
        var queryable = Filter(set.AsQueryable(), filter);
        return await queryable.Select(WorkflowDefinitionSummary.FromDefinitionExpression()).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions.AsNoTracking();
        var queryable = Filter(set.AsQueryable(), filter).OrderBy(order);
        return await queryable.Select(WorkflowDefinitionSummary.FromDefinitionExpression()).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken) =>
        await _store.QueryAsync(queryable => Filter(queryable, filter).OrderByDescending(x => x.Version), OnLoadAsync, cancellationToken).FirstOrDefault();

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default) => await _store.SaveAsync(definition, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(definitions, OnSaveAsync, cancellationToken);

    /// <inheritdoc />
    public async Task<long> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions;
        var queryable = set.AsQueryable();
        var ids = await Filter(queryable, filter).Select(x => x.Id).Distinct().ToListAsync(cancellationToken);
        return await _store.DeleteWhereAsync(x => ids.Contains(x.Id), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => await _store.QueryAsync(queryable => Filter(queryable, filter), cancellationToken).Any();

    /// <inheritdoc />
    public async Task<long> CountDistinctAsync(CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(x => true, x =>  x.DefinitionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> GetIsNameUnique(string name, string? definitionId = default, CancellationToken cancellationToken = default)
    {
        var exists = await _store.AnyAsync(x => x.Name == name && x.DefinitionId != definitionId, cancellationToken);
        return !exists;
    }

    private ValueTask OnSaveAsync(ManagementElsaDbContext managementElsaDbContext, WorkflowDefinition entity, CancellationToken cancellationToken)
    {
        var data = new WorkflowDefinitionState(entity.Options, entity.Variables, entity.Inputs, entity.Outputs, entity.Outcomes, entity.CustomProperties);
        var json = _payloadSerializer.Serialize(data);

        managementElsaDbContext.Entry(entity).Property("Data").CurrentValue = json;
        managementElsaDbContext.Entry(entity).Property("UsableAsActivity").CurrentValue = data.Options.UsableAsActivity;
        return ValueTask.CompletedTask;
    }

    private ValueTask OnLoadAsync(ManagementElsaDbContext managementElsaDbContext, WorkflowDefinition? entity, CancellationToken cancellationToken)
    {
        if (entity == null)
            return ValueTask.CompletedTask;

        var data = new WorkflowDefinitionState(entity.Options, entity.Variables, entity.Inputs, entity.Outputs, entity.Outcomes, entity.CustomProperties);
        var json = (string?)managementElsaDbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
            data = _payloadSerializer.Deserialize<WorkflowDefinitionState>(json);

        entity.Options = data.Options;
        entity.Variables = data.Variables;
        entity.Inputs = data.Inputs;
        entity.Outputs = data.Outputs;
        entity.Outcomes = data.Outcomes;
        entity.CustomProperties = data.CustomProperties;

        return ValueTask.CompletedTask;
    }

    private IQueryable<WorkflowDefinition> Filter(IQueryable<WorkflowDefinition> queryable, WorkflowDefinitionFilter filter)
    {
        if (filter.DefinitionId != null) queryable = queryable.Where(x => x.DefinitionId == filter.DefinitionId);
        if (filter.DefinitionIds != null) queryable = queryable.Where(x => filter.DefinitionIds.Contains(x.DefinitionId));
        if (filter.Id != null) queryable = queryable.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) queryable = queryable.Where(x => filter.Ids.Contains(x.Id));
        if (filter.VersionOptions != null) queryable = queryable.WithVersion(filter.VersionOptions.Value);
        if (filter.MaterializerName != null) queryable = queryable.Where(x => x.MaterializerName == filter.MaterializerName);
        if (filter.Name != null) queryable = queryable.Where(x => x.Name == filter.Name);
        if (filter.Names != null) queryable = queryable.Where(x => filter.Names.Contains(x.Name!));
        if (filter.UsableAsActivity != null) queryable = queryable.Where(x => EF.Property<bool>(x, "UsableAsActivity") == filter.UsableAsActivity);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm)) queryable = queryable.Where(x => x.Name!.Contains(filter.SearchTerm) || x.Description!.Contains(filter.SearchTerm) || x.Id == filter.SearchTerm || x.DefinitionId == filter.SearchTerm);
        return queryable;
    }

    private IQueryable<WorkflowDefinition> Paginate(IQueryable<WorkflowDefinition> queryable, PageArgs? pageArgs)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
        return queryable;
    }
    
    private class WorkflowDefinitionState
    {
        [JsonConstructor]
        public WorkflowDefinitionState()
        {
        }

        public WorkflowDefinitionState(
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