using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Email.Options;
using Elsa.Activities.Email.Services;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Scripting;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Elsa.Activities.Email.Activities
{
    [ActivityDefinition(Category = "Email", Description = "Send an email message.")]
    public class SendEmail : Activity
    {
        private readonly ISmtpService smtpService;
        private readonly IOptions<SmtpOptions> options;

        public SendEmail(ISmtpService smtpService, IOptions<SmtpOptions> options)
        {
            this.smtpService = smtpService;
            this.options = options;
        }

        [ActivityProperty(Hint = "The sender's email address.")]
        public IWorkflowExpression<string> From
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The recipient's email address.")]
        public IWorkflowExpression<string> To
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The subject of the email message.")]
        public IWorkflowExpression<string> Subject
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The body of the email message.")]
        [ExpressionOptions(Multiline = true)]
        public IWorkflowExpression<string> Body
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var from = (await workflowContext.EvaluateAsync(From, cancellationToken)) ?? options.Value.DefaultSender;
            var to = await workflowContext.EvaluateAsync(To, cancellationToken);
            var subject = await workflowContext.EvaluateAsync(Subject, cancellationToken);
            var body = await workflowContext.EvaluateAsync(Body, cancellationToken);
            var message = new MimeMessage();
            
            message.From.Add(new MailboxAddress(@from));
            message.Subject = subject;
            
            message.Body = new TextPart(TextFormat.Html)
            {
                Text = body
            };

            message.To.Add(new MailboxAddress(to));

            await smtpService.SendAsync(message, cancellationToken);

            return Done();
        }
    }
}