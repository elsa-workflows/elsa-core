using Elsa.Samples.TimesheetApproval.Models;

namespace Elsa.Samples.TimesheetApproval.Messages
{
    public abstract class TimesheetMessage
    {
        protected TimesheetMessage(Timesheet timesheet)
        {
            Timesheet = timesheet;
        }
        
        public Timesheet Timesheet { get; }
    }
}