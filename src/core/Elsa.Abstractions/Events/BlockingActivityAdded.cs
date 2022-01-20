using Elsa.Models;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public class BlockingActivityAdded : INotification
    {
        public BlockingActivityAdded(WorkflowExecutionContext workflowExecutionContext, BlockingActivity blockingActivity)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            BlockingActivity = blockingActivity;
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public BlockingActivity BlockingActivity { get; }
    }
}
