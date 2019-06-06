using Elsa.Activities.Primitives.Activities;
using Elsa.Core.Handlers;
using Elsa.Models;
using Elsa.Results;

namespace Elsa.Activities.Primitives.Drivers
{
    public class ForkDriver : ActivityDriver<Fork>
    {
        protected override ActivityExecutionResult OnExecute(Fork activity, WorkflowExecutionContext workflowContext)
        {
            return Endpoints(activity.Forks);
        }
    }
}