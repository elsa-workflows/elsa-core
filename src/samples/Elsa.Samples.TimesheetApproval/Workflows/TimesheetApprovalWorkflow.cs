using Elsa.Activities.Http;
using Elsa.Activities.Http.Services;
using Elsa.Builders;
using Elsa.Samples.TimesheetApproval.Activities;
using Elsa.Samples.TimesheetApproval.Extensions;

namespace Elsa.Samples.TimesheetApproval.Workflows
{
    public class TimesheetApprovalWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<TimesheetSubmitted>()
                .SendEmail(email => email
                    .WithRecipient("manager@acme.com")
                    .WithSubject(context => $"Timesheet received from {context.GetTimesheet().User}")
                    .WithBody(context =>
                    {
                        var timesheet = context.GetTimesheet();
                        var user = timesheet.User;
                        var hours = timesheet.TotalHours;
                        var urlProvider = context.GetService<IAbsoluteUrlProvider>();
                        var timesheetUrl = urlProvider.ToAbsoluteUrl($"/api/timesheets/{timesheet.Id}");
                        return $"Hi!<br>User {user} just submitted their timesheet with a total of {hours} hours. Please <a href=\"{timesheetUrl}\">review</a> and then <a hre\"\">Approve</a> or <a href=\"\">Reject</a>";
                    }));
        }
    }
}