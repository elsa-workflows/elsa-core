using Elsa.Samples.TimesheetApproval.Models;

namespace Elsa.Samples.TimesheetApproval.Messages
{
    public class TimesheetRejected : TimesheetMessage
    {
        public TimesheetRejected(Timesheet timesheet) : base(timesheet)
        {
        }
    }
}