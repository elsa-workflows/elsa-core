using Elsa.Abstractions.Multitenancy;
using Elsa.Models;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public class BlockingActivityRemoved : INotification
    {
        public BlockingActivityRemoved(WorkflowExecutionContext workflowExecutionContext, BlockingActivity blockingActivity, Tenant tenant)
        {
            WorkflowExecutionContext = workflowExecutionContext;
            BlockingActivity = blockingActivity;
            Tenant = tenant;
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public BlockingActivity BlockingActivity { get; }
        public Tenant Tenant { get; }
    }
}