using System;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class SetContextIdBuilderExtensions
    {
        public static IOutcomeBuilder SetContextId(this IBuilder builder, Action<ISetupActivity<SetContextId>> setup) => builder.Then(setup).When(OutcomeNames.Done);
        public static IOutcomeBuilder SetContextId(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string>> value) => builder.SetContextId(activity => activity.Set(x => x.ContextId, value!));
        public static IOutcomeBuilder SetContextId(this IBuilder builder, Func<ActivityExecutionContext, string> value) => builder.SetContextId(activity => activity.Set(x => x.ContextId, value!));
        public static IOutcomeBuilder SetContextId(this IBuilder builder, Func<ValueTask<string>> value) => builder.SetContextId(activity => activity.Set(x => x.ContextId, value!));
        public static IOutcomeBuilder SetContextId(this IBuilder builder, Func<string> value) => builder.SetContextId(activity => activity.Set(x => x.ContextId, value!));
        public static IOutcomeBuilder SetContextId(this IBuilder builder, string value) => builder.SetContextId(activity => activity.Set(x => x.ContextId, value!));
    }
}