using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class FinishBuilderExtensions
    {
        public static IActivityBuilder Finish(this IBuilder builder, Action<ISetupActivity<Finish>>? setup = default) => builder.Then(setup);
        public static IActivityBuilder Finish(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<object?>> output) => builder.Finish(activity => activity.WithOutput(output));
        public static IActivityBuilder Finish(this IBuilder builder, Func<ActivityExecutionContext, object?> output) => builder.Finish(activity => activity.WithOutput(output));
        public static IActivityBuilder Finish(this IBuilder builder, Func<object?> output) => builder.Finish(activity => activity.WithOutput(output));

        public static IActivityBuilder Finish(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string?>> outcome) => builder.Finish(activity => activity.WithOutcome(outcome));
        public static IActivityBuilder Finish(this IBuilder builder, Func<ActivityExecutionContext, string?> outcome) => builder.Finish(activity => activity.WithOutcome(outcome));
        public static IActivityBuilder Finish(this IBuilder builder, Func<string?> outcome) => builder.Finish(activity => activity.WithOutcome(outcome));
        public static IActivityBuilder Finish(this IBuilder builder, string? outcome) => builder.Finish(activity => activity.WithOutcome(outcome));
    }
}