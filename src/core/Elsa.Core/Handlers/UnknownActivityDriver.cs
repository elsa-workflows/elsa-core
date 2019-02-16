using Elsa.Activities;
using Elsa.Models;
using Elsa.Results;
using NodaTime;

namespace Elsa.Handlers
{
    public class UnknownActivityDriver : ActivityDriver<UnknownActivity>
    {
        protected override ActivityExecutionResult OnExecute(UnknownActivity activity, WorkflowExecutionContext workflowContext)
        {
            return Fault($"Unknown activity: {activity.Name}, ID: {activity.Id}");
        }
    }
}