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
        public static OutcomeBuilder ForEach(this IBuilder builder, Action<ForEach> setup, Action<OutcomeBuilder> iteration) => ForEach(builder.Then(setup), iteration);

        public static OutcomeBuilder ForEach(
            this IBuilder builder,
            IWorkflowExpression<ICollection> collection,
            Action<OutcomeBuilder> iteration) =>
            builder.ForEach(a => a.WithCollection(collection), iteration);

        public static OutcomeBuilder ForEach(
            this IBuilder builder,
            Func<ActivityExecutionContext, ICollection> collection,
            Action<OutcomeBuilder> iteration) =>
            builder.ForEach(a => a.WithCollection(collection), iteration);

        public static OutcomeBuilder ForEach(
            this IBuilder builder,
            Func<ICollection> collection,
            Action<OutcomeBuilder> iteration) =>
            builder.ForEach(a => a.WithCollection(collection), iteration);

        public static OutcomeBuilder ForEach(
            this IBuilder builder,
            ICollection collection,
            Action<OutcomeBuilder> iteration) =>
            builder.ForEach(a => a.WithCollection(collection), iteration);
        
        private static OutcomeBuilder ForEach(ActivityBuilder forEach, Action<OutcomeBuilder> iteration)
        {
            iteration.Invoke(forEach.When(OutcomeNames.Iterate));
            return forEach.When(OutcomeNames.Done);
        }
    }
}