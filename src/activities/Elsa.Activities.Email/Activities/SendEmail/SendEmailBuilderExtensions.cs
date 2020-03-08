using System;
using Elsa.Activities.Email;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class SendEmailBuilderExtensions
    {
        public static ActivityBuilder SendEmail(
            this IBuilder builder,
            Action<SendEmail> setup
        ) => builder.Then(setup);
        
        public static ActivityBuilder SendEmail(
            this IBuilder builder,
            IWorkflowExpression<string> from = default,
            IWorkflowExpression<string> to = default,
            IWorkflowExpression<string> subject = default,
            IWorkflowExpression<string> body = default
        ) => builder.Then<SendEmail>(x => x
            .WithSender(from)
            .WithRecipient(to)
            .WithSubject(subject)
            .WithBody(body));

        public static ActivityBuilder SendEmail(
            this IBuilder builder,
            Func<ActivityExecutionContext, string> from = default,
            Func<ActivityExecutionContext, string> to = default,
            Func<ActivityExecutionContext, string> subject = default,
            Func<ActivityExecutionContext, string> body = default
        ) => builder.Then<SendEmail>(x => x
            .WithSender(from)
            .WithRecipient(to)
            .WithSubject(subject)
            .WithBody(body));

        public static ActivityBuilder SendEmail(
            this IBuilder builder,
            Func<string> from = default,
            Func<string> to = default,
            Func<string> subject = default,
            Func<string> body = default
        ) => builder.Then<SendEmail>(x => x
            .WithSender(from)
            .WithRecipient(to)
            .WithSubject(subject)
            .WithBody(body));

        public static ActivityBuilder SendEmail(
            this IBuilder builder,
            string from = default,
            string to = default,
            string subject = default,
            string body = default
        ) => builder.Then<SendEmail>(x => x
            .WithSender(from)
            .WithRecipient(to)
            .WithSubject(subject)
            .WithBody(body));
    }
}