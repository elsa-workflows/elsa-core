using System;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class InstantEventBuilderExtensions
    {
        public static IActivityBuilder InstantEvent(this IBuilder builder, Action<InstantEvent>? setup = default) => builder.Then(setup);
        public static IActivityBuilder InstantEvent(this IBuilder builder, IWorkflowExpression<Instant> instant) => builder.InstantEvent(x => x.WithInstant(instant));
        public static IActivityBuilder InstantEvent(this IBuilder builder, Func<ActivityExecutionContext, Instant> instant) => builder.InstantEvent(x => x.WithInstant(instant));
        public static IActivityBuilder InstantEvent(this IBuilder builder, Func<Instant> instant) => builder.InstantEvent(x => x.WithInstant(instant));
        public static IActivityBuilder InstantEvent(this IBuilder builder, Instant instant) => builder.InstantEvent(x => x.WithInstant(instant));
    }
}