using System;
using System.Runtime.CompilerServices;
using Elsa.Builders;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Email
{
    public static class SendEmailBuilderExtensions
    {
        public static IActivityBuilder SendEmail(this IBuilder builder, Action<ISetupActivity<SendEmail>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder SendEmail(this IBuilder builder, string from, string to, string subject, string body, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendEmail(setup => setup.WithSender(from).WithRecipient(to).WithSubject(subject).WithBody(body), lineNumber, sourceFile);

        public static IActivityBuilder SendEmail(this IBuilder builder, string from, string[] to, string subject, string body, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendEmail(setup => setup.WithSender(from).WithRecipients(to).WithSubject(subject).WithBody(body), lineNumber, sourceFile);

        public static IActivityBuilder SendEmail(this IBuilder builder, string from, string[] to, string[] cc, string subject, string body, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendEmail(setup => setup.WithSender(from).WithRecipients(to).WithCcRecipients(cc).WithSubject(subject).WithBody(body), lineNumber, sourceFile);

        public static IActivityBuilder SendEmail(this IBuilder builder, string from, string[] to, string[] cc, string[] bcc, string subject, string body, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendEmail(setup => setup.WithSender(from).WithRecipients(to).WithCcRecipients(cc).WithBccRecipients(bcc).WithSubject(subject).WithBody(body), lineNumber, sourceFile);
    }
}