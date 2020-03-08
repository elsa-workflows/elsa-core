using Elsa.Samples.TimesheetApproval.Models;
using MediatR;

namespace Elsa.Samples.TimesheetApproval.Messages
{
    public class TimesheetSubmitted : TimesheetMessage, INotification
    {
        public TimesheetSubmitted(Timesheet timesheet) : base(timesheet)
        {
        }
    }
}