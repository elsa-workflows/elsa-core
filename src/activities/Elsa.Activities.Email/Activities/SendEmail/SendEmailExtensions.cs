using System;

using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Email
{
    public static class SendEmailExtensions
    {
        #region sendner
        public static ISetupActivity<SendEmail> WithSender(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.From, value);
        public static ISetupActivity<SendEmail> WithSender(this ISetupActivity<SendEmail> activity, Func<string> value) => activity.Set(x => x.From, value);
        public static ISetupActivity<SendEmail> WithSender(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.From, value);
        #endregion
        #region recipient
        public static ISetupActivity<SendEmail> WithRecipient(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string[]> value) => activity.Set(x => x.To, value);
        public static ISetupActivity<SendEmail> WithRecipient(this ISetupActivity<SendEmail> activity, Func<string[]> value) => activity.Set(x => x.To, value);
        public static ISetupActivity<SendEmail> WithRecipient(this ISetupActivity<SendEmail> activity, string[] value) => activity.Set(x => x.To, value);
        public static ISetupActivity<SendEmail> WithRecipient(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.To, new[] { value });
        #endregion
        #region ccRecipient
        public static ISetupActivity<SendEmail> WithCcRecipient(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string[]> value) => activity.Set(x => x.Cc, value);
        public static ISetupActivity<SendEmail> WithCcRecipient(this ISetupActivity<SendEmail> activity, Func<string[]> value) => activity.Set(x => x.Cc, value);
        public static ISetupActivity<SendEmail> WithCcRecipient(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.Cc, new[] { value });
        public static ISetupActivity<SendEmail> WithCcRecipient(this ISetupActivity<SendEmail> activity, string[] value) => activity.Set(x => x.Cc, value);
        #endregion
        #region bccRecipient
        public static ISetupActivity<SendEmail> WithBccRecipient(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string[]> value) => activity.Set(x => x.Bcc, value);
        public static ISetupActivity<SendEmail> WithBccRecipient(this ISetupActivity<SendEmail> activity, Func<string[]> value) => activity.Set(x => x.Bcc, value);
        public static ISetupActivity<SendEmail> WithBccRecipient(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.Bcc, new[] { value });
        public static ISetupActivity<SendEmail> WithBccRecipient(this ISetupActivity<SendEmail> activity, string[] value) => activity.Set(x => x.Bcc, value);
        #endregion
        #region subject
        public static ISetupActivity<SendEmail> WithSubject(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.Subject, value);
        public static ISetupActivity<SendEmail> WithSubject(this ISetupActivity<SendEmail> activity, Func<string> value) => activity.Set(x => x.Subject, value);
        public static ISetupActivity<SendEmail> WithSubject(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.Subject, value);
        #endregion
        #region body
        public static ISetupActivity<SendEmail> WithBody(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.Body, value);
        public static ISetupActivity<SendEmail> WithBody(this ISetupActivity<SendEmail> activity, Func<string> value) => activity.Set(x => x.Body, value);
        public static ISetupActivity<SendEmail> WithBody(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.Body, value);
        #endregion
    }
}