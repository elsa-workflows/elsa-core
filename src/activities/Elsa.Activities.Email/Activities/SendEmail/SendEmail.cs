using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Email.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using MimeKit;
using MimeKit.Text;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Email
{
    [Action(Category = "Email", Description = "Send an email message.")]
    public class SendEmail : Activity
    {
        private readonly ISmtpService _smtpService;

        public SendEmail(ISmtpService smtpService)
        {
            _smtpService = smtpService;
        }

        [ActivityProperty(Hint = "The sender's email address.")]
        public string From { get; set; }

        [ActivityProperty(Hint = "The recipient's email address.")]
        public string To { get; set; }

        [ActivityProperty(Hint = "The subject of the email message.")]
        public string Subject { get; set; }

        [ActivityProperty(Hint = "The body of the email message.")]
        [WorkflowExpressionOptions(Multiline = true)]
        public string Body { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var message = new MimeMessage();
            
            message.From.Add(MailboxAddress.Parse(From));
            message.Subject = Subject;
            
            message.Body = new TextPart(TextFormat.Html)
            {
                Text = Body
            };

            message.To.Add(MailboxAddress.Parse(To));
            await _smtpService.SendAsync(message, cancellationToken);

            return Done();
        }
    }
}