using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    public static class CorrelateBuilderExtensions
    {
        public static ActivityBuilder Correlate(this IBuilder builder, IWorkflowExpression<string> correlationId) => builder.Then<Correlate>(x => x.WithValue(correlationId));
        public static ActivityBuilder Correlate(this IBuilder builder, Func<ActivityExecutionContext, string> correlationId) => builder.Then<Correlate>(x => x.WithValue(correlationId));
        public static ActivityBuilder Correlate(this IBuilder builder, Func<string> correlationId) => builder.Then<Correlate>(x => x.WithValue(correlationId));
        public static ActivityBuilder Correlate(this IBuilder builder, string correlationId) => builder.Then<Correlate>(x => x.WithValue(correlationId));
    }
}