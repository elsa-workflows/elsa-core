using Elsa.Core.Activities;
using Elsa.Models;
using Elsa.Results;

namespace Elsa.Core.Handlers
{
    public class UnknownActivityDriver : ActivityDriver<UnknownActivity>
    {
        protected override ActivityExecutionResult OnExecute(UnknownActivity activity, WorkflowExecutionContext workflowContext)
        {
            return Fault($"Unknown activity: {activity.TypeName}, ID: {activity.Id}");
        }
    }
}