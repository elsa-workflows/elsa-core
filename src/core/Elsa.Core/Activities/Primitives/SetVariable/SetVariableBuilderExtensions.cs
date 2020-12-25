using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    public static class SetVariableBuilderExtensions
    {
        public static IOutcomeBuilder SetVariable(this IBuilder builder, Action<ISetupActivity<SetVariable>> setup) => builder.Then(setup).When(OutcomeNames.Done);

        public static IOutcomeBuilder SetVariable(this IBuilder builder, string name, Func<ActivityExecutionContext, ValueTask<object?>> value) =>
            builder.SetVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value));
        
        public static IOutcomeBuilder SetVariable(this IBuilder builder, string name, Func<ActivityExecutionContext, object?> value) =>
            builder.SetVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value));
        
        public static IOutcomeBuilder SetVariable(this IBuilder builder, string name, Func<ValueTask<object?>> value) =>
            builder.SetVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value));
        
        public static IOutcomeBuilder SetVariable(this IBuilder builder, string name, Func<object?> value) =>
            builder.SetVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value));
        
        public static IOutcomeBuilder SetVariable(this IBuilder builder, string name, object? value) =>
            builder.SetVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value));

        public static IOutcomeBuilder SetVariable<T>(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<T>> value) => builder.SetVariable(typeof(T).Name, async context => await value(context));
        public static IOutcomeBuilder SetVariable<T>(this IBuilder builder, Func<ActivityExecutionContext, T> value) => builder.SetVariable(typeof(T).Name, context => value(context));
        public static IOutcomeBuilder SetVariable<T>(this IBuilder builder, Func<ValueTask<T>> value) => builder.SetVariable(typeof(T).Name, async () => await value());
        public static IOutcomeBuilder SetVariable<T>(this IBuilder builder, Func<T> value) => builder.SetVariable(typeof(T).Name, () => value());
        public static IOutcomeBuilder SetVariable<T>(this IBuilder builder, T value) => builder.SetVariable(typeof(T).Name, value);
    }
}