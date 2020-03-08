using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Samples.TimesheetApproval.Messages;
using Elsa.Samples.TimesheetApproval.Models;
using MediatR;
using MongoDB.Driver;

namespace Elsa.Samples.TimesheetApproval.Services
{
    public class TimesheetManager
    {
        private readonly IMongoCollection<Timesheet> timesheetCollection;
        private readonly IMediator mediator;

        public TimesheetManager(IMongoCollection<Timesheet> timesheetCollection, IMediator mediator)
        {
            this.timesheetCollection = timesheetCollection;
            this.mediator = mediator;
        }

        public async Task SubmitTimesheet(Timesheet timesheet, CancellationToken cancellationToken = default)
        {
            timesheet.Status = TimesheetStatus.Submitted;
            await SaveAsync(timesheet, cancellationToken);
            await mediator.Publish(new TimesheetSubmitted(timesheet), cancellationToken);
        }
        
        public async Task ApproveTimesheet(Timesheet timesheet, CancellationToken cancellationToken = default)
        {
            timesheet.Status = TimesheetStatus.Approved;
            await SaveAsync(timesheet, cancellationToken);
            await mediator.Publish(new TimesheetApproved(timesheet), cancellationToken);
        }
        
        public async Task RejectTimesheet(Timesheet timesheet, CancellationToken cancellationToken = default)
        {
            timesheet.Status = TimesheetStatus.Rejected;
            await SaveAsync(timesheet, cancellationToken);
            await mediator.Publish(new TimesheetRejected(timesheet), cancellationToken);
        }

        public async Task SaveAsync(Timesheet timesheet, CancellationToken cancellationToken = default)
        {
            if (timesheet.Id == null)
                timesheet.Id = Guid.NewGuid().ToString("N");

            await timesheetCollection.ReplaceOneAsync(
                x => x.Id == timesheet.Id,
                timesheet, new ReplaceOptions
                {
                    IsUpsert = true,
                },
                cancellationToken);
        }
    }
}