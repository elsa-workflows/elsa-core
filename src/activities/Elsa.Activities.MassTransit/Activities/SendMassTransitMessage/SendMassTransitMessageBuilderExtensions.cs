using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class SendMassTransitMessageBuilderExtensions
    {
        public static IActivityBuilder SendMassTransitMessage(this IBuilder builder, Action<ISetupActivity<SendMassTransitMessage>>? setup = default) => builder.Then(setup);
    }
}