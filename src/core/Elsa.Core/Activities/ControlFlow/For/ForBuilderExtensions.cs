using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForBuilderExtensions
    {
        public static IActivityBuilder For(this IBuilder builder, Action<ISetupActivity<For>> setup, Action<IOutcomeBuilder> iterate) => builder.Then(setup, branch => iterate(branch.When(OutcomeNames.Iterate)));

        public static IActivityBuilder For(
            this IBuilder builder,
            Func<ActivityExecutionContext, int> start,
            Func<ActivityExecutionContext, int> end,
            Func<ActivityExecutionContext, int> step,
            Action<IOutcomeBuilder> iterate,
            Operator op = Operator.LessThan) =>
            builder.For(
                activity => activity
                    .Set(x => x.Start, start)
                    .Set(x => x.End, end)
                    .Set(x => x.Step, step)
                    .Set(x => x.Operator, op),
                iterate);

        public static IActivityBuilder For(this IBuilder builder, Func<int> start, Func<int> end, Func<int> step, Action<IOutcomeBuilder> iterate, Operator op = Operator.LessThan) =>
            builder.For(
                activity => activity
                    .Set(x => x.Start, start)
                    .Set(x => x.End, end)
                    .Set(x => x.Step, step)
                    .Set(x => x.Operator, op),
                iterate);

        public static IActivityBuilder For(this IBuilder builder, int start, int end, int step, Action<IOutcomeBuilder> iterate, Operator op = Operator.LessThan) =>
            builder.For(
                activity => activity
                    .Set(x => x.Start, start)
                    .Set(x => x.End, end)
                    .Set(x => x.Step, step)
                    .Set(x => x.Operator, op),
                iterate);

        public static IActivityBuilder For(this IBuilder builder, Func<ActivityExecutionContext, int> start, Func<ActivityExecutionContext, int> end, Action<IOutcomeBuilder> iterate, Operator op = Operator.LessThan) =>
            builder.For(
                activity => activity
                    .Set(x => x.Start, start)
                    .Set(x => x.End, end)
                    .Set(x => x.Operator, op),
                iterate);

        public static IActivityBuilder For(this IBuilder builder, Func<int> start, Func<int> end, Action<IOutcomeBuilder> iterate, Operator op = Operator.LessThan) =>
            builder.For(
                activity => activity
                    .Set(x => x.Start, start)
                    .Set(x => x.End, end)
                    .Set(x => x.Operator, op),
                iterate);

        public static IActivityBuilder For(this IBuilder builder, int start, int end, Action<IOutcomeBuilder> iterate, Operator op = Operator.LessThan) =>
            builder.For(
                activity => activity
                    .Set(x => x.Start, start)
                    .Set(x => x.End, end)
                    .Set(x => x.Operator, op),
                iterate);
    }
}