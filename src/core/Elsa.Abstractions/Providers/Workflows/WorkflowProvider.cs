using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Providers.Workflow;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class WorkflowProvider : IWorkflowProvider
    {
        public virtual async IAsyncEnumerable<IWorkflowBlueprint> GetWorkflowsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var workflows = await OnGetWorkflowsAsync(cancellationToken);
            
            foreach (var workflow in workflows)
                yield return workflow;
        }
        
        protected virtual ValueTask<IEnumerable<IWorkflowBlueprint>> OnGetWorkflowsAsync(CancellationToken cancellationToken) => new(new IWorkflowBlueprint[0]);
    }
}