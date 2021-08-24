using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Email
{
    public static class SendEmailExtensions
    {
        public static ISetupActivity<SendEmail> WithSender(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.From, value);
        public static ISetupActivity<SendEmail> WithSender(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.From, value);
        public static ISetupActivity<SendEmail> WithSender(this ISetupActivity<SendEmail> activity, Func<string> value) => activity.Set(x => x.From, value);
        public static ISetupActivity<SendEmail> WithSender(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.From, value);


        public static ISetupActivity<SendEmail> WithRecipients(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, ValueTask<ICollection<string>?>> value) => activity.Set(x => x.To, value);
        public static ISetupActivity<SendEmail> WithRecipients(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string[]> value) => activity.Set(x => x.To, value);
        public static ISetupActivity<SendEmail> WithRecipients(this ISetupActivity<SendEmail> activity, Func<string[]> value) => activity.Set(x => x.To, value);
        public static ISetupActivity<SendEmail> WithRecipients(this ISetupActivity<SendEmail> activity, string[] value) => activity.Set(x => x.To, value);

        
        public static ISetupActivity<SendEmail> WithRecipient(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.To, context => new[] { value(context) });
        public static ISetupActivity<SendEmail> WithRecipient(this ISetupActivity<SendEmail> activity, Func<string> value) => activity.Set(x => x.To, () => new[] { value() });
        public static ISetupActivity<SendEmail> WithRecipient(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.To, new[] { value });

        public static ISetupActivity<SendEmail> WithCcRecipients(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, ValueTask<ICollection<string>?>> value) => activity.Set(x => x.Cc, value);
        public static ISetupActivity<SendEmail> WithCcRecipients(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string[]> value) => activity.Set(x => x.Cc, value);
        public static ISetupActivity<SendEmail> WithCcRecipients(this ISetupActivity<SendEmail> activity, Func<string[]> value) => activity.Set(x => x.Cc, value);
        public static ISetupActivity<SendEmail> WithCcRecipient(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.Cc, new[] { value });
        public static ISetupActivity<SendEmail> WithCcRecipients(this ISetupActivity<SendEmail> activity, string[] value) => activity.Set(x => x.Cc, value);

        public static ISetupActivity<SendEmail> WithBccRecipients(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, ValueTask<ICollection<string>?>> value) => activity.Set(x => x.Bcc, value);
        public static ISetupActivity<SendEmail> WithBccRecipients(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string[]> value) => activity.Set(x => x.Bcc, value);
        public static ISetupActivity<SendEmail> WithBccRecipients(this ISetupActivity<SendEmail> activity, Func<string[]> value) => activity.Set(x => x.Bcc, value);
        public static ISetupActivity<SendEmail> WithBccRecipient(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.Bcc, new[] { value });
        public static ISetupActivity<SendEmail> WithBccRecipients(this ISetupActivity<SendEmail> activity, string[] value) => activity.Set(x => x.Bcc, value);


        public static ISetupActivity<SendEmail> WithSubject(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.Subject, value);
        public static ISetupActivity<SendEmail> WithSubject(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.Subject, value);
        public static ISetupActivity<SendEmail> WithSubject(this ISetupActivity<SendEmail> activity, Func<string> value) => activity.Set(x => x.Subject, value);
        public static ISetupActivity<SendEmail> WithSubject(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.Subject, value);

        public static ISetupActivity<SendEmail> WithBody(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.Body, value);
        public static ISetupActivity<SendEmail> WithBody(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.Body, value);
        public static ISetupActivity<SendEmail> WithBody(this ISetupActivity<SendEmail> activity, Func<string> value) => activity.Set(x => x.Body, value);
        public static ISetupActivity<SendEmail> WithBody(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.Body, value);
        
        public static ISetupActivity<SendEmail> WithAttachments(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, ValueTask<object?>> value) => activity.Set(x => x.Attachments, value);
        public static ISetupActivity<SendEmail> WithAttachments(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, object?> value) => activity.Set(x => x.Attachments, value);
        public static ISetupActivity<SendEmail> WithAttachments(this ISetupActivity<SendEmail> activity, Func<object?> value) => activity.Set(x => x.Attachments, value);
        public static ISetupActivity<SendEmail> WithAttachments(this ISetupActivity<SendEmail> activity, object? value) => activity.Set(x => x.Attachments, value);
    }
}