using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    public static class SetTransientVariableBuilderExtensions
    {
        public static IActivityBuilder SetTransientVariable(this IBuilder builder, Action<ISetupActivity<SetTransientVariable>> setup) => builder.Then(setup);

        public static IActivityBuilder SetTransientVariable(this IBuilder builder, string name, Func<ActivityExecutionContext, ValueTask<object?>> value) =>
            builder.SetTransientVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value));
        
        public static IActivityBuilder SetTransientVariable(this IBuilder builder, string name, Func<ActivityExecutionContext, object?> value) =>
            builder.SetTransientVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value));
        
        public static IActivityBuilder SetTransientVariable(this IBuilder builder, string name, Func<ValueTask<object?>> value) =>
            builder.SetTransientVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value));
        
        public static IActivityBuilder SetTransientVariable(this IBuilder builder, string name, Func<object?> value) =>
            builder.SetTransientVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value));
        
        public static IActivityBuilder SetTransientVariable(this IBuilder builder, string name, object? value) =>
            builder.SetTransientVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value));

        public static IActivityBuilder SetTransientVariable<T>(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<T>> value) => builder.SetTransientVariable(typeof(T).Name, async context => await value(context));
        public static IActivityBuilder SetTransientVariable<T>(this IBuilder builder, Func<ActivityExecutionContext, T> value) => builder.SetTransientVariable(typeof(T).Name, context => value(context));
        public static IActivityBuilder SetTransientVariable<T>(this IBuilder builder, Func<ValueTask<T>> value) => builder.SetTransientVariable(typeof(T).Name, async () => await value());
        public static IActivityBuilder SetTransientVariable<T>(this IBuilder builder, Func<T> value) => builder.SetTransientVariable(typeof(T).Name, () => value());
        public static IActivityBuilder SetTransientVariable<T>(this IBuilder builder, T value) => builder.SetTransientVariable(typeof(T).Name, value);
    }
}