using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowDefinitionStore : InMemoryWorkflowStoreBase, IWorkflowDefinitionStore
    {
        public InMemoryWorkflowDefinitionStore(IInMemoryWorkflowProvider provider) : base(provider)
        {
        }

        public async Task<IEnumerable<Tuple<Workflow, IActivity>>> ListByStartActivityAsync(string activityType, CancellationToken cancellationToken)
        {
            var workflows = await ListAllAsync(cancellationToken);
            var query =
                from workflow in workflows
                from activity in workflow.GetStartActivities()
                where activity.TypeName == activityType
                select Tuple.Create(workflow, activity);

            return query.Distinct();
        }
    }
}