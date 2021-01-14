using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    public static class SetVariableBuilderExtensions
    {
        public static IActivityBuilder SetVariable(this IBuilder builder, Action<ISetupActivity<SetVariable>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder SetVariable(this IBuilder builder, string name, Func<ActivityExecutionContext, ValueTask<object?>> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable(this IBuilder builder, string name, Func<ActivityExecutionContext, object?> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable(this IBuilder builder, string name, Func<ValueTask<object?>> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable(this IBuilder builder, string name, Func<object?> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable(this IBuilder builder, string name, object? value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(
                activity => activity
                    .Set(x => x.VariableName, name)
                    .Set(x => x.Value, value), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<T>> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(typeof(T).Name, async context => await value(context), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, Func<ActivityExecutionContext, T> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(typeof(T).Name, context => value(context), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, Func<ValueTask<T>> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(typeof(T).Name, async () => await value(), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, Func<T> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(typeof(T).Name, () => value(), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, T value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(typeof(T).Name, value, lineNumber, sourceFile);
    }
}