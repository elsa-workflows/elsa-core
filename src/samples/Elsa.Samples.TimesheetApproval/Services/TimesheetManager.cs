using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Samples.TimesheetApproval.Messages;
using Elsa.Samples.TimesheetApproval.Models;
using MediatR;
using YesSql;

namespace Elsa.Samples.TimesheetApproval.Services
{
    public class TimesheetManager
    {
        private readonly ISession _session;
        private readonly IMediator _mediator;

        public TimesheetManager(ISession session, IMediator mediator)
        {
            _session = session;
            _mediator = mediator;
        }

        public async Task SubmitTimesheet(Timesheet timesheet, CancellationToken cancellationToken = default)
        {
            timesheet.Status = TimesheetStatus.Submitted;
            await SaveAsync(timesheet, cancellationToken);
            await _mediator.Publish(new TimesheetSubmitted(timesheet), cancellationToken);
        }
        
        public async Task ApproveTimesheet(Timesheet timesheet, CancellationToken cancellationToken = default)
        {
            timesheet.Status = TimesheetStatus.Approved;
            await SaveAsync(timesheet, cancellationToken);
            await _mediator.Publish(new TimesheetApproved(timesheet), cancellationToken);
        }
        
        public async Task RejectTimesheet(Timesheet timesheet, CancellationToken cancellationToken = default)
        {
            timesheet.Status = TimesheetStatus.Rejected;
            await SaveAsync(timesheet, cancellationToken);
            await _mediator.Publish(new TimesheetRejected(timesheet), cancellationToken);
        }

        public async Task SaveAsync(Timesheet timesheet, CancellationToken cancellationToken = default)
        {
            if (timesheet.TimesheetId == null)
                timesheet.TimesheetId = Guid.NewGuid().ToString("N");

            _session.Save(timesheet);
        }
    }
}