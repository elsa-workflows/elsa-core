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
    public async Task<WorkflowDefinition?> FindByIdAsync(string id, CancellationToken cancellationToken = default) =>
        await _store.FindAsync(x => x.Id == id, Load, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.DefinitionId == definitionId;
        predicate = predicate.WithVersion(versionOptions);
        return await _store.FindManyAsync(predicate, Load, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.DefinitionId == definitionId;
        predicate = predicate.WithVersion(versionOptions);
        return await _store.FindAsync(predicate, Load, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.Name == name;
        predicate = predicate.WithVersion(versionOptions);
        return await _store.FindAsync(predicate, Load, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindWorkflowsWithActivityBehaviourAsync(CancellationToken cancellationToken = default)
    {
        return await _store.FindManyAsync(w => w.UsableAsActivity == true && w.IsPublished, Load, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionSummary>> FindManySummariesAsync(IEnumerable<string> definitionIds, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions;
        var query = set.AsQueryable();

        if (versionOptions != null)
            query = query.WithVersion(versionOptions.Value);

        query = query.Where(x => definitionIds.Contains(x.DefinitionId));

        return query.OrderBy(x => x.Name).Select(x => WorkflowDefinitionSummary.FromDefinition(Load(dbContext, x)!)).ToList();
    }

    public async Task<WorkflowDefinition?> FindPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.DefinitionId == definitionId;
        predicate = predicate.WithVersion(VersionOptions.Published);
        return await _store.FindAsync(predicate, Load, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) =>
        await _store.FindManyAsync(x => x.DefinitionId == definitionId && (x.IsLatest || x.IsPublished), Load, cancellationToken);

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindLastVersionByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions;
        var query = set.AsQueryable();
        return Load(dbContext, query.Where(w => w.DefinitionId == definitionId).OrderByDescending(w => w.Version).FirstOrDefault());
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowDefinition record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, Save, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowDefinition> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, Save, cancellationToken);

    /// <inheritdoc />
    public async Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        await _workflowInstanceStore.DeleteWhereAsync(x => x.DefinitionId == definitionId, cancellationToken);
        return await _store.DeleteWhereAsync(x => x.DefinitionId == definitionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteByDefinitionIdAndVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        await _workflowInstanceStore.DeleteWhereAsync(x => x.DefinitionId == definitionId && x.Version == version, cancellationToken);
        return await _store.DeleteWhereAsync(x => x.DefinitionId == definitionId && x.Version == version, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.ToList();
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        await _workflowInstanceStore.DeleteWhereAsync(x => definitionIdList.Contains(x.DefinitionId), cancellationToken);
        return await _store.DeleteWhereAsync(x => definitionIdList.Contains(x.DefinitionId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowDefinitionSummary>> ListSummariesAsync(
        VersionOptions? versionOptions = default,
        string? materializerName = default,
        PageArgs? pageArgs = default,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var workflowDefinitions = dbContext.WorkflowDefinitions;
        var query = workflowDefinitions.AsQueryable();

        if (versionOptions != null) query = query.WithVersion(versionOptions.Value);
        if (!string.IsNullOrWhiteSpace(materializerName)) query = query.Where(x => x.MaterializerName == materializerName);

        query = query.OrderBy(x => x.Name);

        return await query.PaginateAsync(x => WorkflowDefinitionSummary.FromDefinition(x), pageArgs);
    }

    /// <inheritdoc />
    public async Task<bool> GetExistsAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.DefinitionId == definitionId;
        predicate = predicate.WithVersion(versionOptions);
        return await _store.AnyAsync(predicate, cancellationToken);
    }

    private WorkflowDefinition Save(ManagementElsaDbContext managementElsaDbContext, WorkflowDefinition entity)
    {
        var data = new WorkflowDefinitionState(entity.Options, entity.Variables, entity.CustomProperties);
        var options = _serializerOptionsProvider.CreatePersistenceOptions();
        var json = JsonSerializer.Serialize(data, options);

        managementElsaDbContext.Entry(entity).Property("Data").CurrentValue = json;
        return entity;
    }

    private WorkflowDefinition? Load(ManagementElsaDbContext managementElsaDbContext, WorkflowDefinition? entity)
    {
        if (entity == null)
            return null;

        var data = new WorkflowDefinitionState(entity.Options, entity.Variables, entity.CustomProperties);
        var json = (string?)managementElsaDbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var options = _serializerOptionsProvider.CreatePersistenceOptions();
            data = JsonSerializer.Deserialize<WorkflowDefinitionState>(json, options)!;
        }

        entity.Options = data.Options;
        entity.Variables = data.Variables;
        entity.CustomProperties = data.CustomProperties;

        return entity;
    }

    private class WorkflowDefinitionState
    {
        public WorkflowDefinitionState()
        {
        }

        public WorkflowDefinitionState(WorkflowOptions? options, ICollection<Variable> variables, IDictionary<string, object> customProperties)
        {
            Options = options;
            Variables = variables;
            CustomProperties = customProperties;
        }

        public WorkflowOptions? Options { get; set; }
        public ICollection<Variable> Variables { get; set; } = new List<Variable>();
        public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    }
}