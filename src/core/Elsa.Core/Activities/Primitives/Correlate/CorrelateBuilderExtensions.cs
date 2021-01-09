using System;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class CorrelateBuilderExtensions
    {
        public static IActivityBuilder Correlate(this IBuilder builder, Action<ISetupActivity<Correlate>> setup) => builder.Then(setup);
        public static IActivityBuilder Correlate(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string?>> value) => builder.Correlate(activity => activity.Set(x => x.Value, value));
        public static IActivityBuilder Correlate(this IBuilder builder, Func<ActivityExecutionContext, string?> value) => builder.Correlate(activity => activity.Set(x => x.Value, value));
        public static IActivityBuilder Correlate(this IBuilder builder, Func<ValueTask<string?>> value) => builder.Correlate(activity => activity.Set(x => x.Value, value));
        public static IActivityBuilder Correlate(this IBuilder builder, Func<string?> value) => builder.Correlate(activity => activity.Set(x => x.Value, value));
        public static IActivityBuilder Correlate(this IBuilder builder, string? value) => builder.Correlate(activity => activity.Set(x => x.Value, value));
    }
}