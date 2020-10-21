using System;
using System.Collections.Generic;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForEachBuilderExtensions
    {
        public static IActivityBuilder ForEach(
            this IBuilder builder,
            Action<ISetupActivity<ForEach>> setup,
            Action<IOutcomeBuilder> iterate) =>
            builder.Then(setup, branch => iterate(branch.When(OutcomeNames.Iterate)));

        public static IActivityBuilder ForEach(
            this IBuilder builder,
            Func<ActivityExecutionContext, ICollection<object>> items,
            Action<IOutcomeBuilder> iterate) =>
            builder.ForEach(activity => activity.Set(x => x.Items, items), iterate);

        public static IActivityBuilder ForEach(
            this IBuilder builder,
            Func<ICollection<object>> items,
            Action<IOutcomeBuilder> iterate) =>
            builder.ForEach(activity => activity.Set(x => x.Items, items), iterate);

        public static IActivityBuilder ForEach(
            this IBuilder builder,
            ICollection<object> items,
            Action<IOutcomeBuilder> iterate) =>
            builder.ForEach(activity => activity.Set(x => x.Items, items), iterate);
    }
}