using Elsa.Workflows.Management.Entities;

namespace Elsa.Retention.Contracts;

/// <summary>
///     A generic variant of <see cref="IRelatedEntityCollector{TEntity}" />
/// </summary>
public interface IRelatedEntityCollector
{
    IAsyncEnumerable<ICollection<object>> GetRelatedEntitiesGeneric(ICollection<WorkflowInstance> workflowInstances);
}

/// <summary>
///     Collects <see cref="TEntity" /> that are related to the workflow instance
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IRelatedEntityCollector<TEntity> : IRelatedEntityCollector where TEntity : class
{
    async IAsyncEnumerable<ICollection<object>> IRelatedEntityCollector.GetRelatedEntitiesGeneric(ICollection<WorkflowInstance> workflowInstances)
    {
        await foreach (ICollection<TEntity> entity in GetRelatedEntities(workflowInstances).ConfigureAwait(false))
        {
            yield return entity.Select(x => (object)x).ToArray();
        }
    }

    /// <summary>
    ///     Collects the entities related to the given workflow instances
    /// </summary>
    /// <param name="workflowInstances"></param>
    /// <returns></returns>
    IAsyncEnumerable<ICollection<TEntity>> GetRelatedEntities(ICollection<WorkflowInstance> workflowInstances);
}