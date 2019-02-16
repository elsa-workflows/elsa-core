using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Email.Activities;
using Elsa.Handlers;
using Elsa.Models;
using Elsa.Results;

namespace Elsa.Activities.Email.Drivers
{
    public class SendEmailDriver : ActivityDriver<SendEmail>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly SmtpClient smtpClient;

        public SendEmailDriver(IWorkflowExpressionEvaluator expressionEvaluator, SmtpClient smtpClient)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.smtpClient = smtpClient;
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(SendEmail activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var from = await expressionEvaluator.EvaluateAsync(activity.From, workflowContext, cancellationToken);
            var to = await expressionEvaluator.EvaluateAsync(activity.To, workflowContext, cancellationToken);
            var subject = await expressionEvaluator.EvaluateAsync(activity.Subject, workflowContext, cancellationToken);
            var body = await expressionEvaluator.EvaluateAsync(activity.Body, workflowContext, cancellationToken);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(@from),
                Body = body,
                Subject = subject
            };

            mailMessage.To.Add(to);
            smtpClient.SendAsync(mailMessage, null);

            return Endpoint("Done");
        }
    }
}