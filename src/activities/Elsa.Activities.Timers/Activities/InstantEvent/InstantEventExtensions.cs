using System;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class InstantEventExtensions
    {
        public static InstantEvent WithInstant(this InstantEvent activity, IWorkflowExpression<Instant> value) => activity.With(x => x.Instant, value);
        public static InstantEvent WithInstant(this InstantEvent activity, Func<ActivityExecutionContext, Instant> value) => activity.With(x => x.Instant, new CodeExpression<Instant>(value));
        public static InstantEvent WithInstant(this InstantEvent activity, Func<Instant> value) => activity.With(x => x.Instant, new CodeExpression<Instant>(value));
        public static InstantEvent WithInstant(this InstantEvent activity, Instant value) => activity.With(x => x.Instant, new CodeExpression<Instant>(value));
    }
}