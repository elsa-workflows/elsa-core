using System;
using Elsa.Activities.Email;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class SendEmailExtensions
    {
        public static ISetupActivity<SendEmail> WithSender(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.From, value);
        public static ISetupActivity<SendEmail> WithSender(this ISetupActivity<SendEmail> activity, Func<string> value) => activity.Set(x => x.From, value);
        public static ISetupActivity<SendEmail> WithSender(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.From, value);
        public static ISetupActivity<SendEmail> WithRecipient(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.To, value);
        public static ISetupActivity<SendEmail> WithRecipient(this ISetupActivity<SendEmail> activity, Func<string> value) => activity.Set(x => x.To, value);
        public static ISetupActivity<SendEmail> WithRecipient(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.To, value);
        public static ISetupActivity<SendEmail> WithSubject(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.Subject, value);
        public static ISetupActivity<SendEmail> WithSubject(this ISetupActivity<SendEmail> activity, Func<string> value) => activity.Set(x => x.Subject, value);
        public static ISetupActivity<SendEmail> WithSubject(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.Subject, value);
        public static ISetupActivity<SendEmail> WithBody(this ISetupActivity<SendEmail> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.Body, value);
        public static ISetupActivity<SendEmail> WithBody(this ISetupActivity<SendEmail> activity, Func<string> value) => activity.Set(x => x.Body, value);
        public static ISetupActivity<SendEmail> WithBody(this ISetupActivity<SendEmail> activity, string value) => activity.Set(x => x.Body, value);
    }
}