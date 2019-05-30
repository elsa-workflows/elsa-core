using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IInMemoryWorkflowProvider provider;

        public InMemoryWorkflowDefinitionStore(IInMemoryWorkflowProvider provider)
        {
            this.provider = provider;
        }

        public async Task<IEnumerable<Workflow>> ListAllAsync(CancellationToken cancellationToken)
        {
            return await provider.ListAsync(cancellationToken);
        }

        public async Task SaveAsync(Workflow value, CancellationToken cancellationToken)
        {
            await provider.SaveAsync(value, cancellationToken);
        }

        public async Task<IEnumerable<Tuple<Workflow, IActivity>>> ListByStartActivityAsync(string activityType, CancellationToken cancellationToken)
        {
            var workflows = await ListAllAsync(cancellationToken);
            var query =
                from workflow in workflows
                from activity in workflow.GetStartActivities()
                where activity.Name == activityType
                select Tuple.Create(workflow, activity);

            return query.Distinct();
        }

        public async Task<Workflow> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            var workflows = await ListAllAsync(cancellationToken);

            return workflows.FirstOrDefault(x => x.Id == id);
        }
    }
}