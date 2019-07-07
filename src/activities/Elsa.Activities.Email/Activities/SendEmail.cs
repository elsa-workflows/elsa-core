using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Core.Expressions;
using Elsa.Core.Extensions;
using Elsa.Core.Services;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Email.Activities
{
    public class SendEmail : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly SmtpClient smtpClient;

        public SendEmail(IWorkflowExpressionEvaluator expressionEvaluator, SmtpClient smtpClient)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.smtpClient = smtpClient;
        }

        public WorkflowExpression<string> From
        {
            get => GetState(() => new WorkflowExpression<string>(PlainTextEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        public WorkflowExpression<string> To
        {
            get => GetState(() => new WorkflowExpression<string>(PlainTextEvaluator.SyntaxName, ""));  
            set => SetState(value);
        }

        public WorkflowExpression<string> Subject
        {
            get => GetState(() => new WorkflowExpression<string>(PlainTextEvaluator.SyntaxName, "")); 
            set => SetState(value);
        }

        public WorkflowExpression<string> Body
        {
            get => GetState(() => new WorkflowExpression<string>(PlainTextEvaluator.SyntaxName, "")); 
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var from = await expressionEvaluator.EvaluateAsync(From, workflowContext, cancellationToken);
            var to = await expressionEvaluator.EvaluateAsync(To, workflowContext, cancellationToken);
            var subject = await expressionEvaluator.EvaluateAsync(Subject, workflowContext, cancellationToken);
            var body = await expressionEvaluator.EvaluateAsync(Body, workflowContext, cancellationToken);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(@from),
                Body = body,
                Subject = subject,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);
            await smtpClient.SendMailAsync(mailMessage);

            return Done();
        }
    }
}