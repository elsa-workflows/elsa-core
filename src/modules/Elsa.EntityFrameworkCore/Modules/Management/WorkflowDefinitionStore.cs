using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.EntityFrameworkCore;
using Open.Linq.AsyncExtensions;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Elsa.Common.Entities;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using JetBrains.Annotations;

namespace Elsa.EntityFrameworkCore.Modules.Management;

/// <inheritdoc />
[UsedImplicitly]
public class EFCoreWorkflowDefinitionStore(EntityStore<ManagementElsaDbContext, WorkflowDefinition> store, IPayloadSerializer payloadSerializer)
    : IWorkflowDefinitionStore
{
    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var orderBy = new WorkflowDefinitionOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending);
        return await FindAsync(filter, orderBy, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), OnLoadAsync, filter.TenantAgnostic, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var orderBy = new WorkflowDefinitionOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending);
        return await FindManyAsync(filter, orderBy, pageArgs, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), cancellationToken).LongCount();
        var results = await store.QueryAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), OnLoadAsync, filter.TenantAgnostic, cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var orderBy = new WorkflowDefinitionOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending);
        return await FindManyAsync(filter, orderBy, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(queryable => Filter(queryable, filter).OrderBy(order), OnLoadAsync, filter.TenantAgnostic, cancellationToken).ToList();
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The method 'FindSummariesAsync' is used for serialization and requires unreferenced code to be preserved.")]
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var orderBy = new WorkflowDefinitionOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending);
        return await FindSummariesAsync(filter, orderBy, pageArgs, cancellationToken);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The method 'FindSummariesAsync' is used for serialization and requires unreferenced code to be preserved.")]
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions.AsNoTracking();
        var queryable = Filter(set.AsQueryable(), filter).OrderBy(order);
        
        if (filter.TenantAgnostic)
            queryable = queryable.IgnoreQueryFilters();

        var count = await queryable.LongCountAsync(cancellationToken);
        queryable = Paginate(queryable, pageArgs);
        var results = await queryable.Select(WorkflowDefinitionSummary.FromDefinitionExpression()).ToListAsync(cancellationToken);
        return Page.Of(results, count);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The method 'FindSummariesAsync' is used for serialization and requires unreferenced code to be preserved.")]
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var orderBy = new WorkflowDefinitionOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending);
        return await FindSummariesAsync(filter, orderBy, cancellationToken);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The method 'FindSummariesAsync' is used for serialization and requires unreferenced code to be preserved.")]
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions.AsNoTracking();
        var queryable = Filter(set.AsQueryable(), filter).OrderBy(order);
        
        if (filter.TenantAgnostic)
            queryable = queryable.IgnoreQueryFilters();
        
        return await queryable.Select(WorkflowDefinitionSummary.FromDefinitionExpression()).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        return await store.QueryAsync(queryable => Filter(queryable, filter).OrderByDescending(x => x.Version), OnLoadAsync, filter.TenantAgnostic, cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        await store.SaveAsync(definition, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
    {
        await store.SaveManyAsync(definitions, OnSaveAsync, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions;
        var queryable = set.AsQueryable();
        
        if (filter.TenantAgnostic)
            queryable = queryable.IgnoreQueryFilters();
        
        var ids = await Filter(queryable, filter).Select(x => x.Id).Distinct().ToListAsync(cancellationToken);
        return await store.DeleteWhereAsync(x => ids.Contains(x.Id), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(queryable => Filter(queryable, filter), filter.TenantAgnostic, cancellationToken).Any();
    }

    /// <inheritdoc />
    public async Task<long> CountDistinctAsync(CancellationToken cancellationToken = default)
    {
        return await store.CountAsync(x => true, x => x.DefinitionId, false, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> GetIsNameUnique(string name, string? definitionId = default, CancellationToken cancellationToken = default)
    {
        var exists = await store.AnyAsync(x => x.Name == name && x.DefinitionId != definitionId, false, cancellationToken);
        return !exists;
    }

    private ValueTask OnSaveAsync(ManagementElsaDbContext managementElsaDbContext, WorkflowDefinition entity, CancellationToken cancellationToken)
    {
        var data = new WorkflowDefinitionState(entity.Options, entity.Variables, entity.Inputs, entity.Outputs, entity.Outcomes, entity.CustomProperties);
        var json = payloadSerializer.Serialize(data);

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
            data = payloadSerializer.Deserialize<WorkflowDefinitionState>(json);

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
        var definitionId = filter.DefinitionId ?? filter.DefinitionHandle?.DefinitionId;
        var versionOptions = filter.VersionOptions ?? filter.DefinitionHandle?.VersionOptions;
        var id = filter.Id ?? filter.DefinitionHandle?.DefinitionVersionId;
        
        if (definitionId != null) queryable = queryable.Where(x => x.DefinitionId == definitionId);
        if (filter.DefinitionIds != null) queryable = queryable.Where(x => filter.DefinitionIds.Contains(x.DefinitionId));
        if (id != null) queryable = queryable.Where(x => x.Id == id);
        if (filter.Ids != null) queryable = queryable.Where(x => filter.Ids.Contains(x.Id));
        if (versionOptions != null) queryable = queryable.WithVersion(versionOptions.Value);
        if (filter.MaterializerName != null) queryable = queryable.Where(x => x.MaterializerName == filter.MaterializerName);
        if (filter.Name != null) queryable = queryable.Where(x => x.Name == filter.Name);
        if (filter.Names != null) queryable = queryable.Where(x => filter.Names.Contains(x.Name!));
        if (filter.UsableAsActivity != null) queryable = queryable.Where(x => EF.Property<bool>(x, "UsableAsActivity") == filter.UsableAsActivity);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm)) queryable = queryable.Where(x => x.Name!.Contains(filter.SearchTerm) || x.Description!.Contains(filter.SearchTerm) || x.Id.Contains(filter.SearchTerm) || x.DefinitionId.Contains(filter.SearchTerm));

        // TEMP: IsSystem may be null when upgrading from older versions of Elsa to 3.2. See issue #5366.
        // In a future version, we should remove this check and simply do queryable.Where(x => x.IsSystem == filter.IsSystem).
        if (filter.IsSystem != null)
            queryable = filter.IsSystem == true
                ? queryable.Where(x => x.IsSystem == true)
                : queryable.Where(x => x.IsSystem == false || x.IsSystem == null);

        if (filter.IsReadonly != null) queryable = queryable.Where(x => x.IsReadonly == filter.IsReadonly);
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