using System;
using Elsa.Activities.Email;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class SendEmailBuilderExtensions
    {
        public static IActivityBuilder SendEmail(this IBuilder builder,
            Action<ISetupActivity<SendEmail>> setup) => builder.Then(setup);
    }
}