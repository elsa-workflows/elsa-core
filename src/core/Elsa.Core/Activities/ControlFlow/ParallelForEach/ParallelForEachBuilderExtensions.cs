using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ParallelForEachBuilderExtensions
    {
        public static IActivityBuilder ParallelForEach(
            this IBuilder builder,
            Action<ISetupActivity<ParallelForEach>> setup,
            Action<IOutcomeBuilder> iterate) =>
            builder.Then(setup, branch => iterate(branch.When(OutcomeNames.Iterate)));

        public static IActivityBuilder ParallelForEach(
            this IBuilder builder,
            Func<ActivityExecutionContext, ICollection<object>> items,
            Action<IOutcomeBuilder> iterate) =>
            builder.ParallelForEach(activity => activity.Set(x => x.Items, items), iterate);

        public static IActivityBuilder ParallelForEach(
            this IBuilder builder,
            Func<ICollection<object>> items,
            Action<IOutcomeBuilder> iterate) =>
            builder.ParallelForEach(activity => activity.Set(x => x.Items, items), iterate);

        public static IActivityBuilder ParallelForEach(
            this IBuilder builder,
            ICollection<object> items,
            Action<IOutcomeBuilder> iterate) =>
            builder.ParallelForEach(activity => activity.Set(x => x.Items, items), iterate);
    }
}