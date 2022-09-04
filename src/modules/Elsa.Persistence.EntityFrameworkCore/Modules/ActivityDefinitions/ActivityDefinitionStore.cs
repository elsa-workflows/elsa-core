using System.Linq.Expressions;
using System.Text.Json;
using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Models;
using Elsa.ActivityDefinitions.Services;
using Elsa.Common.Extensions;
using Elsa.Common.Models;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.ActivityDefinitions;

public class EFCoreActivityDefinitionStore : IActivityDefinitionStore
{
    private readonly Store<ActivityDefinitionsDbContext, ActivityDefinition> _store;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;

    public EFCoreActivityDefinitionStore(Store<ActivityDefinitionsDbContext, ActivityDefinition> store, SerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    public async Task<IEnumerable<ActivityDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) =>
        await _store.FindManyAsync(x => x.DefinitionId == definitionId && (x.IsLatest || x.IsPublished), cancellationToken);

    public async Task SaveAsync(ActivityDefinition record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);

    public async Task<Page<ActivityDefinition>> ListAsync(VersionOptions? versionOptions = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var workflowDefinitions = dbContext.ActivityDefinitions;
        var query = workflowDefinitions.AsQueryable();

        if (versionOptions != null) query = query.WithVersion(versionOptions.Value);

        query = query.OrderBy(x => x.DisplayName).ThenBy(x => x.Type);

        return await query.PaginateAsync(pageArgs);
    }

    public async Task<Page<ActivityDefinitionSummary>> ListSummariesAsync(VersionOptions? versionOptions = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var workflowDefinitions = dbContext.ActivityDefinitions;
        var query = workflowDefinitions.AsQueryable();

        if (versionOptions != null) query = query.WithVersion(versionOptions.Value);

        query = query.OrderBy(x => x.DisplayName).ThenBy(x => x.Type);

        return await query.PaginateAsync(x => ActivityDefinitionSummary.FromDefinition(x), pageArgs);
    }

    public async Task<ActivityDefinition?> FindByTypeAsync(string type, int version, CancellationToken cancellationToken = default) =>
        await _store.FindAsync(x => x.Type == type && x.Version == version, cancellationToken);

    public async Task<ActivityDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        Expression<Func<ActivityDefinition, bool>> predicate = x => x.DefinitionId == definitionId;
        predicate = predicate.WithVersion(versionOptions);
        return await _store.FindAsync(predicate, cancellationToken);
    }

    public async Task<ActivityDefinition?> FindByDefinitionVersionIdAsync(string definitionVersionId, CancellationToken cancellationToken = default) =>
        await _store.FindAsync(x => x.Id == definitionVersionId, cancellationToken);

    public async Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) =>
        await _store.DeleteWhereAsync(x => x.DefinitionId == definitionId, cancellationToken);

    public async Task<int> DeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.ToList();
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        return await _store.DeleteWhereAsync(x => definitionIdList.Contains(x.DefinitionId), cancellationToken);
    }

    public ActivityDefinition Save(ActivityDefinitionsDbContext dbContext, ActivityDefinition entity)
    {
        var data = new
        {
            entity.Variables,
            entity.Metadata,
            entity.ApplicationProperties
        };

        var options = _serializerOptionsProvider.CreatePersistenceOptions();
        var json = JsonSerializer.Serialize(data, options);

        dbContext.Entry(entity).Property("Data").CurrentValue = json;
        return entity;
    }

    public ActivityDefinition? Load(ActivityDefinitionsDbContext dbContext, ActivityDefinition? entity)
    {
        if (entity == null)
            return null;
        
        var data = new ActivityDefinitionState(entity.Variables, entity.Metadata, entity.ApplicationProperties);
        var json = (string?)dbContext.Entry(entity).Property("Data").CurrentValue;

        if (!string.IsNullOrWhiteSpace(json))
        {
            var options = _serializerOptionsProvider.CreatePersistenceOptions();
            data = JsonSerializer.Deserialize<ActivityDefinitionState>(json, options)!;
        }

        entity.Variables = data.Variables;
        entity.Metadata = data.Metadata;
        entity.ApplicationProperties = data.ApplicationProperties;

        return entity;
    }

    // Can't use records when using System.Text.Json serialization and reference handling. Hence, using a class with default constructor.
    private class ActivityDefinitionState
    {
        public ActivityDefinitionState()
        {
        }

        public ActivityDefinitionState(
            ICollection<Variable> variables,
            IDictionary<string, object> metadata,
            IDictionary<string, object> applicationProperties)
        {
            Variables = variables;
            Metadata = metadata;
            ApplicationProperties = applicationProperties;
        }

        public ICollection<Variable> Variables { get; set; } = new List<Variable>();
        public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public IDictionary<string, object> ApplicationProperties { get; set; } = new Dictionary<string, object>();
    }
}