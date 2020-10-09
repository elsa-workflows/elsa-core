using System.Threading;
using System.Threading.Tasks;
using Elsa.Samples.TimesheetApproval.Messages;
using Elsa.Samples.TimesheetApproval.Models;
using Elsa.Services;
using MediatR;

namespace Elsa.Samples.TimesheetApproval.Handlers
{
    public class TimesheetEventsHandler : INotificationHandler<TimesheetSubmitted>
    {
        private readonly IWorkflowScheduler _workflowScheduler;

        public TimesheetEventsHandler(IWorkflowScheduler workflowScheduler)
        {
            this._workflowScheduler = workflowScheduler;
        }

        public async Task Handle(TimesheetSubmitted notification, CancellationToken cancellationToken) => 
            await TriggerWorkflowsAsync<Activities.TimesheetSubmitted>(notification.Timesheet, cancellationToken);

        private async Task TriggerWorkflowsAsync<T>(Timesheet timesheet, CancellationToken cancellationToken) where T:IActivity => 
            await _workflowScheduler.TriggerWorkflowsAsync(typeof(T).Name, timesheet, timesheet.Id, cancellationToken: cancellationToken);
    }
}