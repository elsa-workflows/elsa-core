using System;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Email
{
    public static class SendEmailBuilderExtensions
    {
        public static IActivityBuilder SendEmail(this IBuilder builder, Action<ISetupActivity<SendEmail>> setup) => builder.Then(setup);
        public static IActivityBuilder SendEmail(this IBuilder builder, string from, string to, string subject, string body) => builder.SendEmail(setup => setup.WithSender(from).WithRecipient(to).WithSubject(subject).WithBody(body));
    }
}