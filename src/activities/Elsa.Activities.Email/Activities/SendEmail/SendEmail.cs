using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Email.Options;
using Elsa.Activities.Email.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Email
{
    [Action(Category = "Email", Description = "Send an email message.")]
    public class SendEmail : Activity
    {
        private readonly ISmtpService _smtpService;
        private readonly SmtpOptions _options;

        public SendEmail(ISmtpService smtpService, IOptions<SmtpOptions> options)
        {
            _smtpService = smtpService;
            _options = options.Value;
        }

        [ActivityInput(Hint = "The sender's email address.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? From { get; set; }

        [ActivityInput(Hint = "The recipients email addresses.", UIHint = ActivityInputUIHints.MultiText, DefaultSyntax = SyntaxNames.Json, SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript })]
        public ICollection<string> To { get; set; } = new List<string>();

        [ActivityInput(
            Hint = "The cc recipients email addresses.", 
            UIHint = ActivityInputUIHints.MultiText, 
            DefaultSyntax = SyntaxNames.Json, 
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript },
            Category = "More")]
        public ICollection<string> Cc { get; set; } = new List<string>();

        [ActivityInput(
            Hint = "The Bcc recipients email addresses.", 
            UIHint = ActivityInputUIHints.MultiText, 
            DefaultSyntax = SyntaxNames.Json, 
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript },
            Category = "More")]
        public ICollection<string> Bcc { get; set; } = new List<string>();

        [ActivityInput(Hint = "The subject of the email message.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? Subject { get; set; }

        [ActivityInput(Hint = "The body of the email message.", UIHint = ActivityInputUIHints.MultiLine, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? Body { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var message = new MimeMessage();
            var from = From is null or "" ? _options.DefaultSender : From;

            message.From.Add(MailboxAddress.Parse(from));
            message.Subject = Subject;

            message.Body = new TextPart(TextFormat.Html)
            {
                Text = Body
            };

            SetRecipientsEmailAddresses(message.To, To);
            SetRecipientsEmailAddresses(message.Cc, Cc);
            SetRecipientsEmailAddresses(message.Bcc, Bcc);

            await _smtpService.SendAsync(message, context.CancellationToken);

            return Done();
        }

        private void SetRecipientsEmailAddresses(InternetAddressList list, IEnumerable<string>? addresses)
        {
            if(addresses == null)
                return;
            
            list.AddRange(addresses.Select(MailboxAddress.Parse));
        }
    }
}