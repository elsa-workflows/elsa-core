using Elsa.Samples.TimesheetApproval.Models;
using Elsa.Services.Models;

namespace Elsa.Samples.TimesheetApproval.Extensions
{
    public static class ActivityContextExtensions
    {
        public static Timesheet GetTimesheet(this ActivityExecutionContext context) => context.GetVariable<Timesheet>("Timesheet");
    }
}