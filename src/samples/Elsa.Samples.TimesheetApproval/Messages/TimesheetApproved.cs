using Elsa.Samples.TimesheetApproval.Models;

namespace Elsa.Samples.TimesheetApproval.Messages
{
    public class TimesheetApproved : TimesheetMessage
    {
        public TimesheetApproved(Timesheet timesheet) : base(timesheet)
        {
        }
    }
}