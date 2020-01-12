using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class CancelScheduledMassTransitMessageBuilderExtensions
    {
        public static ActivityBuilder CancelScheduledMassTransitMessage(this IBuilder builder, Action<CancelScheduledMassTransitMessage>? setup = default) => builder.Then(setup);
        public static ActivityBuilder CancelScheduledMassTransitMessage(this IBuilder builder, IWorkflowExpression<Guid> value) => builder.CancelScheduledMassTransitMessage(x => x.WithTokenId(value));
        public static ActivityBuilder CancelScheduledMassTransitMessage(this IBuilder builder, Func<ActivityExecutionContext, Guid> value) => builder.CancelScheduledMassTransitMessage(x => x.WithTokenId(value));
        public static ActivityBuilder CancelScheduledMassTransitMessage(this IBuilder builder, Func<Guid> value) => builder.CancelScheduledMassTransitMessage(x => x.WithTokenId(value));
        public static ActivityBuilder CancelScheduledMassTransitMessage(this IBuilder builder, Guid value) => builder.CancelScheduledMassTransitMessage(x => x.WithTokenId(value));
    }
}