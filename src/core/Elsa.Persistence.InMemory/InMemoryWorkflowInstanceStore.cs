using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private readonly IInMemoryWorkflowProvider provider;

        public InMemoryWorkflowInstanceStore(IInMemoryWorkflowProvider provider)
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

        public async Task<IEnumerable<Tuple<Workflow, IActivity>>> ListByBlockingActivityAsync(string workflowType, CancellationToken cancellationToken)
        {
            var workflows = await provider.ListAsync(cancellationToken);
            var query =
                from workflow in workflows
                from activity in workflow.BlockingActivities
                where activity.Name == workflowType
                select Tuple.Create(workflow, activity);

            return query.Distinct();
        }
    }
}