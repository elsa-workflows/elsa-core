using Elsa.Models;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public class BlockingActivityRemoved : INotification
    {
        public BlockingActivityRemoved(WorkflowExecutionContext workflowExecutionContext, BlockingActivity blockingActivity)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            BlockingActivity = blockingActivity;
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public BlockingActivity BlockingActivity { get; }
    }
}