using System;
using Elsa.Builders;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class ScheduleSendMassTransitMessageBuilderExtensions
    {
        public static IActivityBuilder ScheduleSendMassTransitMessage(this IBuilder builder, Action<ISetupActivity<ScheduleSendMassTransitMessage>>? setup = default) => builder.Then(setup);
    }
}