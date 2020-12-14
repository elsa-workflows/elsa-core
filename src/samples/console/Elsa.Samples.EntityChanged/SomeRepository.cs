using System.Threading.Tasks;
using Elsa.Activities.Entity;
using Elsa.Activities.Entity.Extensions;
using Elsa.Services;

namespace Elsa.Samples.EntityChanged
{
    public class SomeRepository
    {
        private readonly IWorkflowRunner _workflowRunner;

        public SomeRepository(IWorkflowRunner workflowRunner) => _workflowRunner = workflowRunner;
        public Task AddAsync(Entity entity) => TriggerWorkflowsAsync(entity, EntityChangedAction.Added);
        public Task DeleteAsync(Entity entity) => TriggerWorkflowsAsync(entity, EntityChangedAction.Deleted);

        private async Task TriggerWorkflowsAsync(Entity entity, EntityChangedAction changedAction) =>
            await _workflowRunner.TriggerEntityChangedWorkflowsAsync(
                entity.Id,
                entity.GetType().GetEntityName(),
                changedAction,
                entity.Id,
                entity.Id,
                entity);
    }
}