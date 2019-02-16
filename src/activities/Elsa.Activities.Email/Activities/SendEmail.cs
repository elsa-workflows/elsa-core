using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Activities.Email.Activities
{
    public class SendEmail : Activity
    {
        public WorkflowExpression<string> From { get; set; }
        public WorkflowExpression<string> To { get; set; }
        public WorkflowExpression<string> Subject { get; set; }
        public WorkflowExpression<string> Body { get; set; }
    }
}