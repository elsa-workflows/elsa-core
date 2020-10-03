using Elsa.ActivityResults;
using Elsa.Samples.TimesheetApproval.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Samples.TimesheetApproval.Activities
{
    public class TimesheetSubmitted : Activity
    {
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var timesheet = (Timesheet) context.Input;

            context.SetVariable("Timesheet", timesheet);
            return Done();
        }
    }
}