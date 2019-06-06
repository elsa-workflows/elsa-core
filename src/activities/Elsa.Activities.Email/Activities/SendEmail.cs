using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Email.Activities
{
    [ActivityDisplayName("Send Email")]
    [ActivityCategory("Email")]
    [ActivityDescription("Send an email message via SMTP.")]
    public class SendEmail : Activity
    {
        public WorkflowExpression<string> From { get; set; }
        public WorkflowExpression<string> To { get; set; }
        public WorkflowExpression<string> Subject { get; set; }
        public WorkflowExpression<string> Body { get; set; }
    }
}