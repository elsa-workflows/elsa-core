using System;
using Elsa.Activities.Email;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class SendEmailExtensions
    {
        public static SendEmail WithSender(this SendEmail activity, IWorkflowExpression<string> value) => activity.With(x => x.From, value);
        public static SendEmail WithSender(this SendEmail activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.From, new CodeExpression<string>(value));
        public static SendEmail WithSender(this SendEmail activity, Func<string> value) => activity.With(x => x.From, new CodeExpression<string>(value));
        public static SendEmail WithSender(this SendEmail activity, string value) => activity.With(x => x.From, new CodeExpression<string>(value));
        public static SendEmail WithRecipient(this SendEmail activity, IWorkflowExpression<string> value) => activity.With(x => x.To, value);
        public static SendEmail WithRecipient(this SendEmail activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.To, new CodeExpression<string>(value));
        public static SendEmail WithRecipient(this SendEmail activity, Func<string> value) => activity.With(x => x.To, new CodeExpression<string>(value));
        public static SendEmail WithRecipient(this SendEmail activity, string value) => activity.With(x => x.To, new CodeExpression<string>(value));
        public static SendEmail WithSubject(this SendEmail activity, IWorkflowExpression<string> value) => activity.With(x => x.Subject, value);
        public static SendEmail WithSubject(this SendEmail activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.Subject, new CodeExpression<string>(value));
        public static SendEmail WithSubject(this SendEmail activity, Func<string> value) => activity.With(x => x.Subject, new CodeExpression<string>(value));
        public static SendEmail WithSubject(this SendEmail activity, string value) => activity.With(x => x.Subject, new CodeExpression<string>(value));
        public static SendEmail WithBody(this SendEmail activity, IWorkflowExpression<string> value) => activity.With(x => x.Body, value);
        public static SendEmail WithBody(this SendEmail activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.Body, new CodeExpression<string>(value));
        public static SendEmail WithBody(this SendEmail activity, Func<string> value) => activity.With(x => x.Body, new CodeExpression<string>(value));
        public static SendEmail WithBody(this SendEmail activity, string value) => activity.With(x => x.Body, new CodeExpression<string>(value));
    }
}