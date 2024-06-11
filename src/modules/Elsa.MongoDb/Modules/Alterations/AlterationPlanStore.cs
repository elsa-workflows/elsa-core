using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Filters;
using Elsa.MongoDb.Common;
using Elsa.MongoDb.Modules.Alterations.Documents;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver.Linq;

namespace Elsa.MongoDb.Modules.Alterations;

/// <summary>
/// A MongoDb implementation of <see cref="IAlterationPlanStore"/>.
/// </summary>
public class MongoAlterationPlanStore : IAlterationPlanStore
{
    private readonly MongoDbStore<AlterationPlanDocument> _mongoDbStore;
    private readonly IAlterationSerializer _alterationSerializer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MongoAlterationPlanStore(IServiceProvider serviceProvider,
        IAlterationSerializer alterationSerializer)
    {
        // Resolved from IServiceProvider instead of injecting through the constructor so AlterationPlanDocument can be internal instead of public.
        _mongoDbStore = serviceProvider.GetRequiredService<MongoDbStore<AlterationPlanDocument>>();
        _alterationSerializer = alterationSerializer;
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default)
    {
        return await _mongoDbStore.CountAsync(queryable => Filter(queryable, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AlterationPlan?> FindAsync(AlterationPlanFilter filter, CancellationToken cancellationToken = default)
    {
        var document = await _mongoDbStore.FindAsync(queryable => Filter(queryable, filter), cancellationToken);

        if (document == null) return null;

        return Map(document);
    }

    /// <inheritdoc />
    public async Task SaveAsync(AlterationPlan plan, CancellationToken cancellationToken = default)
    {
        var document = Map(plan);

        await _mongoDbStore.SaveAsync(document, cancellationToken);
    }

    private static IMongoQueryable<AlterationPlanDocument> Filter(IMongoQueryable<AlterationPlanDocument> queryable, AlterationPlanFilter filter)
    {
        return (Apply(queryable, filter) as IMongoQueryable<AlterationPlanDocument>)!;
    }

    private static IQueryable<AlterationPlanDocument> Apply(IQueryable<AlterationPlanDocument> queryable, AlterationPlanFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Id))
            queryable = queryable.Where(x => x.Id == filter.Id);

        return queryable;
    }

    private AlterationPlan Map(AlterationPlanDocument document)
    {
        return new AlterationPlan
        {
            Id = document.Id,
            Alterations = _alterationSerializer.DeserializeMany(document.Alterations).ToList(),
            WorkflowInstanceFilter = document.WorkflowInstanceFilter,
            Status = document.Status,
            CreatedAt = document.CreatedAt,
            StartedAt = document.StartedAt,
            CompletedAt = document.CompletedAt
        };
    }

    private AlterationPlanDocument Map(AlterationPlan plan)
    {
        return new AlterationPlanDocument
        {
            Id = plan.Id,
            Alterations = _alterationSerializer.SerializeMany(plan.Alterations),
            WorkflowInstanceFilter = plan.WorkflowInstanceFilter,
            Status = plan.Status,
            CreatedAt = plan.CreatedAt,
            StartedAt = plan.StartedAt,
            CompletedAt = plan.CompletedAt
        };
    }
}