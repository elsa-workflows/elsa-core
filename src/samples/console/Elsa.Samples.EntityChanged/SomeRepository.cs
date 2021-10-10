using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Entity;
using Elsa.Activities.Entity.Extensions;
using Elsa.Services;

namespace Elsa.Samples.EntityChanged
{
    public class SomeRepository
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        private readonly ICollection<Entity> _collection = new List<Entity>();

        public SomeRepository(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;
        public Task AddAsync(Entity entity)
        {
            _collection.Add(entity);
            return TriggerWorkflowsAsync(entity, EntityChangedAction.Added);
        }

        public Task DeleteAsync(Entity entity)
        {
            _collection.Remove(entity);
            return TriggerWorkflowsAsync(entity, EntityChangedAction.Deleted);
        }
        
        public Task<Entity?> GetAsync(string id) => Task.FromResult(_collection.FirstOrDefault(x => x.Id == id));

        private async Task TriggerWorkflowsAsync(Entity entity, EntityChangedAction changedAction) =>
            await _workflowLaunchpad.TriggerEntityChangedWorkflowsAsync(
                entity.Id,
                entity.GetType().GetEntityName(),
                changedAction,
                entity.Id,
                entity.Id);
    }
}