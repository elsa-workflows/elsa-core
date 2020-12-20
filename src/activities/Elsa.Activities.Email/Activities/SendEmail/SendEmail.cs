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
        public string? From { get; set; }

        [ActivityProperty(Hint = "The recipients email addresses.")]
        public string[] To { get; set; } = default!;

        [ActivityProperty(Hint = "The cc recipients email addresses. (Optional)")]
        public string[]? Cc { get; set; }

        [ActivityProperty(Hint = "The Bcc recipients email addresses. (Optional)")]
        public string[]? Bcc { get; set; }

        [ActivityProperty(Hint = "The subject of the email message.")]
        public string? Subject { get; set; }

        [ActivityProperty(Hint = "The body of the email message.")]
        [WorkflowExpressionOptions(Multiline = true)]
        public string? Body { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var message = new MimeMessage();

            message.From.Add(MailboxAddress.Parse(From));
            message.Subject = Subject;

            message.Body = new TextPart(TextFormat.Html)
            {
                Text = Body
            };

            SetRecipiensEmailAddresses(message.To, To);
            SetRecipiensEmailAddresses(message.Cc, Cc);
            SetRecipiensEmailAddresses(message.Bcc, Bcc);
           
            await _smtpService.SendAsync(message, context.CancellationToken);

            return Done();
        }

        private void SetRecipiensEmailAddresses(InternetAddressList list, string[]? addresses)
        {
            if (addresses == null)
            {
                return;
            }

            for (var i = 0; i < addresses.Length; i++)
            {
                list.Add(MailboxAddress.Parse(addresses[i]));
            }
        }
    }
}