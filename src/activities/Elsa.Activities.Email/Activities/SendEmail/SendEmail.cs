using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Email.Options;
using Elsa.Activities.Email.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Email
{
    [ActivityDefinition(Category = "Email", Description = "Send an email message.")]
    public class SendEmail : Activity
    {
        private readonly ISmtpService _smtpService;
        private readonly IOptions<SmtpOptions> _options;

        public SendEmail(ISmtpService smtpService, IOptions<SmtpOptions> options)
        {
            this._smtpService = smtpService;
            this._options = options;
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

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
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