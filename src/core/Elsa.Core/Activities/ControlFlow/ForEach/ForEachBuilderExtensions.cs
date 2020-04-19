using System;
using System.Collections;
using System.Collections.Generic;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForEachBuilderExtensions
    {
        public static IOutcomeBuilder ForEach(this IBuilder builder, Action<ForEach> setup, Action<IOutcomeBuilder> iteration) => ForEach(builder.Then(setup), iteration);

        public static IOutcomeBuilder ForEach(
            this IBuilder builder,
            IWorkflowExpression<ICollection> collection,
            Action<IOutcomeBuilder> iteration) =>
            builder.ForEach(a => a.WithCollection(collection), iteration);

        public static IOutcomeBuilder ForEach(
            this IBuilder builder,
            Func<ActivityExecutionContext, ICollection> collection,
            Action<IOutcomeBuilder> iteration) =>
            builder.ForEach(a => a.WithCollection(collection), iteration);

        public static IOutcomeBuilder ForEach(
            this IBuilder builder,
            Func<ICollection> collection,
            Action<IOutcomeBuilder> iteration) =>
            builder.ForEach(a => a.WithCollection(collection), iteration);

        public static IOutcomeBuilder ForEach(
            this IBuilder builder,
            ICollection collection,
            Action<IOutcomeBuilder> iteration) =>
            builder.ForEach(a => a.WithCollection(collection), iteration);
        
        private static IOutcomeBuilder ForEach(IActivityBuilder forEach, Action<IOutcomeBuilder> iteration)
        {
            iteration.Invoke(forEach.When(OutcomeNames.Iterate));
            return forEach.When(OutcomeNames.Done);
        }
    }
}