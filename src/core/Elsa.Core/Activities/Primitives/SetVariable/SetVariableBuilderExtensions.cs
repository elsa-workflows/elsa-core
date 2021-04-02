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

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, string name, Func<ActivityExecutionContext, ValueTask<T?>> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(activity => activity.WithVariableName(name).WithValue(async x => (object?) await value(x)), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, string name, Func<ActivityExecutionContext, T?> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(activity => activity.WithVariableName(name).WithValue(x => (object?) value(x)), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, string name, Func<ValueTask<T?>> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(activity => activity.WithVariableName(name).WithValue(async () => (object?) await value()), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, string name, Func<T?> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(activity => activity.WithVariableName(name).WithValue(() => (object?) value()), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, string name, T? value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(activity => activity.WithVariableName(name).WithValue(() => (object?)value), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<T?>> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(typeof(T).Name, async context => await value(context), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, Func<ActivityExecutionContext, T?> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(typeof(T).Name, value, lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, Func<ValueTask<T?>> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) where T : class =>
            builder.SetVariable(typeof(T).Name, async () => await value(), lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, Func<T?> value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) where T : class =>
            builder.SetVariable(typeof(T).Name, value, lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(this IBuilder builder, T? value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) where T : class =>
            builder.SetVariable(typeof(T).Name, value, lineNumber, sourceFile);
    }
}