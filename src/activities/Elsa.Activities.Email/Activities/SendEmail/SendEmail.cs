using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Email.Options;
using Elsa.Activities.Email.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Options;
using MimeKit;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Email
{
    [Action(Category = "Email", Description = "Send an email message.")]
    public class SendEmail : Activity
    {
        private readonly ISmtpService _smtpService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IContentSerializer _contentSerializer;
        private readonly SmtpOptions _options;

        public SendEmail(ISmtpService smtpService, IOptions<SmtpOptions> options, IHttpClientFactory httpClientFactory, IContentSerializer contentSerializer)
        {
            _smtpService = smtpService;
            _httpClientFactory = httpClientFactory;
            _contentSerializer = contentSerializer;
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

        [ActivityInput(
            Hint = "The attachments to send with the email message. Can be (an array of) a fully-qualified file path, URL, stream, byte array or instances of EmailAttachment.",
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public object? Attachments { get; set; }

        [ActivityInput(Hint = "The body of the email message.", UIHint = ActivityInputUIHints.MultiLine, SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? Body { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var message = new MimeMessage();
            var from = string.IsNullOrWhiteSpace(From) ? _options.DefaultSender : From;
            
            message.Sender = MailboxAddress.Parse(from);
            message.Subject = Subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = Body };
            await AddAttachmentsAsync(bodyBuilder, cancellationToken);

            message.Body = bodyBuilder.ToMessageBody();

            SetRecipientsEmailAddresses(message.To, To);
            SetRecipientsEmailAddresses(message.Cc, Cc);
            SetRecipientsEmailAddresses(message.Bcc, Bcc);

            await _smtpService.SendAsync(context, message, context.CancellationToken);

            return Done();
        }

        private async Task AddAttachmentsAsync(BodyBuilder bodyBuilder, CancellationToken cancellationToken)
        {
            var attachments = Attachments;

            if (attachments != null)
            {
                var index = 0;
                var attachmentObjects = InterpretAttachmentsModel(attachments);

                foreach (var attachmentObject in attachmentObjects)
                {
                    switch (attachmentObject)
                    {
                        case Uri url:
                            await AttachOnlineFileAsync(bodyBuilder, url, cancellationToken);
                            break;
                        case string path when path.Contains("://"):
                            await AttachOnlineFileAsync(bodyBuilder, new Uri(path), cancellationToken);
                            break;
                        case string path:
                            await AttachLocalFileAsync(bodyBuilder, path, cancellationToken);
                            break;
                        case byte[] bytes:
                        {
                            var fileName = $"Attachment-{++index}";
                            var contentType = "application/binary";
                            bodyBuilder.Attachments.Add(fileName, bytes, ContentType.Parse(contentType));
                            break;
                        } 
                        case Stream stream:
                        {
                            var fileName = $"Attachment-{++index}";
                            var contentType = "application/binary";
                            await bodyBuilder.Attachments.AddAsync(fileName, stream, ContentType.Parse(contentType), cancellationToken);
                            break;
                        } 
                        case EmailAttachment emailAttachment:
                        {
                            var fileName = emailAttachment.FileName ?? $"Attachment-{++index}";
                            var contentType = emailAttachment.ContentType ?? "application/binary";
                            bodyBuilder.Attachments.Add(fileName, emailAttachment.Content, ContentType.Parse(contentType));
                            break;
                        }
                        default:
                        {
                            var json = _contentSerializer.Serialize(attachmentObject);
                            var fileName = $"Attachment-{++index}";
                            var contentType = "application/json";
                            bodyBuilder.Attachments.Add(fileName, Encoding.UTF8.GetBytes(json), ContentType.Parse(contentType));
                            break;
                        }
                    }
                }
            }
        }

        private async Task AttachLocalFileAsync(BodyBuilder bodyBuilder, string path, CancellationToken cancellationToken) => await bodyBuilder.Attachments.AddAsync(path, cancellationToken);

        private async Task AttachOnlineFileAsync(BodyBuilder bodyBuilder, Uri url, CancellationToken cancellationToken)
        {
            var fileName = Path.GetFileName(url.LocalPath);
            var response = await DownloadUrlAsync(url);
            var contentStream = await response.Content.ReadAsStreamAsync();
            var contentType = response.Content.Headers.ContentType.MediaType;
            await bodyBuilder.Attachments.AddAsync(fileName, contentStream, ContentType.Parse(contentType), cancellationToken);
        }

        private IEnumerable InterpretAttachmentsModel(object attachments) => attachments is string text ? new[] { text } : attachments is IEnumerable enumerable ? enumerable : new[] { attachments };

        private void SetRecipientsEmailAddresses(InternetAddressList list, IEnumerable<string>? addresses)
        {
            if (addresses == null)
                return;

            list.AddRange(addresses.Select(MailboxAddress.Parse));
        }

        private async Task<HttpResponseMessage> DownloadUrlAsync(Uri url)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(url);
            return response;
        }
    }
}