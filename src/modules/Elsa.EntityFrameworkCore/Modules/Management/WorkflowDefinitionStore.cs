using System.Data.Entity;
using System.Linq.Expressions;
using System.Text.Json;
using Elsa.Common.Models;
using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.Extensions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Modules.Management;

/// <inheritdoc />
public class EFCoreWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly Store<ManagementElsaDbContext, WorkflowDefinition> _store;
    private readonly Store<ManagementElsaDbContext, WorkflowInstance> _workflowInstanceStore;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EFCoreWorkflowDefinitionStore(
        Store<ManagementElsaDbContext, WorkflowDefinition> store,
        Store<ManagementElsaDbContext, WorkflowInstance> workflowInstanceStore,
        SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _workflowInstanceStore = workflowInstanceStore;
        _serializerOptionsProvider = serializerOptionsProvider;
    }
    
    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) =>
        await _store.QueryAsync(queryable => Filter(queryable, filter), Load, cancellationToken).FirstOrDefault();

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await _store.QueryAsync(queryable => Filter(queryable, filter), cancellationToken).LongCount();
        var results = await _store.QueryAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), Load, cancellationToken).ToList();
        return new(results, count);
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => 
        await _store.QueryAsync(queryable => Filter(queryable, filter), Load, cancellationToken).ToList();

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions;
        var queryable = Filter(set.AsQueryable(), filter);
        var count = await queryable.LongCountAsync(cancellationToken);
        var results = await queryable.Select(x => WorkflowDefinitionSummary.FromDefinition(x)).ToListAsync(cancellationToken);

        return new(results, count);
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions;
        var queryable = Filter(set.AsQueryable(), filter);
        return await queryable.Select(x => WorkflowDefinitionSummary.FromDefinition(x)).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken) =>
        await _store.QueryAsync(queryable => Filter(queryable, filter).OrderByDescending(x => x.Version), cancellationToken).FirstOrDefault();

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinition record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, Save, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinition> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, Save, cancellationToken);

    /// <inheritdoc />
    public async Task<int> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions;
        var queryable = set.AsQueryable();
        var definitionIds = await Filter(queryable, filter).Select(x => x.DefinitionId).Distinct().ToListAsync(cancellationToken);
        
        await _workflowInstanceStore.DeleteWhereAsync(x => definitionIds.Contains(x.DefinitionId), cancellationToken);
        return await _store.DeleteWhereAsync(x => definitionIds.Contains(x.DefinitionId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default) => await _store.QueryAsync(queryable => Filter(queryable, filter), cancellationToken).Any();

    private WorkflowDefinition Save(ManagementElsaDbContext managementElsaDbContext, WorkflowDefinition entity)
    {
        var data = new WorkflowDefinitionState(entity.Options, entity.Variables, entity.Inputs, entity.Outputs, entity.CustomProperties);
        var options = _serializerOptionsProvider.CreatePersistenceOptions();
        var json = JsonSerializer.Serialize(data, options);

        managementElsaDbContext.Entry(entity).Property("Data").CurrentValue = json;
        return entity;
    }

    private WorkflowDefinition? Load(ManagementElsaDbContext managementElsaDbContext, WorkflowDefinition? entity)
    {
        if (entity == null)
            return null;

        var data = new WorkflowDefinitionState(entity.Options, entity.Variables, entity.Inputs, entity.Outputs, entity.CustomProperties);
        var json = (string?)managementElsaDbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var options = _serializerOptionsProvider.CreatePersistenceOptions();
            data = JsonSerializer.Deserialize<WorkflowDefinitionState>(json, options)!;
        }

        entity.Options = data.Options;
        entity.Variables = data.Variables;
        entity.Inputs = data.Inputs;
        entity.Outputs = data.Outputs;
        entity.CustomProperties = data.CustomProperties;

        return entity;
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
        if (filter.Names != null) queryable = queryable.Where(x => filter.Names.Contains(x.Name));
        if (filter.UsableAsActivity != null) queryable = queryable.Where(x => x.UsableAsActivity == filter.UsableAsActivity);

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
        public WorkflowDefinitionState()
        {
        }

        public WorkflowDefinitionState(
            WorkflowOptions? options,
            ICollection<Variable> variables,
            ICollection<InputDefinition> inputs,
            ICollection<OutputDefinition> outputs,
            IDictionary<string, object> customProperties
        )
        {
            Options = options;
            Variables = variables;
            Inputs = inputs;
            Outputs = outputs;
            CustomProperties = customProperties;
        }

        public WorkflowOptions? Options { get; set; }
        public ICollection<Variable> Variables { get; set; } = new List<Variable>();
        public ICollection<InputDefinition> Inputs { get; set; } = new List<InputDefinition>();
        public ICollection<OutputDefinition> Outputs { get; set; } = new List<OutputDefinition>();
        public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    }
}