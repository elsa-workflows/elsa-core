using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Services;
using MediatR;

namespace Elsa.Handlers
{
    public class FlushWorkflowExecutionLog : INotificationHandler<WorkflowInstanceSaved>
    {
        private readonly IWorkflowExecutionLog _workflowExecutionLog;
        public FlushWorkflowExecutionLog(IWorkflowExecutionLog workflowExecutionLog) => _workflowExecutionLog = workflowExecutionLog;
        public async Task Handle(WorkflowInstanceSaved notification, CancellationToken cancellationToken) => await _workflowExecutionLog.FlushAsync(cancellationToken);
    }
}