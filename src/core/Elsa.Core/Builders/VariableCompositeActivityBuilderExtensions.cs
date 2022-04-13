using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa.Builders
{
    public static class VariableCompositeActivityBuilderExtensions
    {
        public static IActivityBuilder SetVariable<T>(
            this ICompositeActivityBuilder builder,
            string variableName,
            Func<ActivityExecutionContext, ValueTask<object?>> value,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.StartWith<SetVariable>(
                activity =>
                {
                    activity.WithVariableName(variableName);
                    activity.WithValue(value);
                }, null, lineNumber, sourceFile);


        public static IActivityBuilder SetVariable<T>(
            this ICompositeActivityBuilder builder,
            string variableName,
            Func<ActivityExecutionContext, object?> value,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.StartWith<SetVariable>(
                activity =>
                {
                    activity.WithVariableName(variableName);
                    activity.WithValue(value);
                }, null, lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(
            this ICompositeActivityBuilder builder,
            string variableName,
            Func<object?> value,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.StartWith<SetVariable>(
                activity =>
                {
                    activity.WithVariableName(variableName);
                    activity.WithValue(value);
                }, null, lineNumber, sourceFile);

        public static IActivityBuilder SetVariable<T>(
            this ICompositeActivityBuilder builder,
            string variableName,
            object? value,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.StartWith<SetVariable>(
                activity =>
                {
                    activity.WithVariableName(variableName);
                    activity.WithValue(value);
                }, null, lineNumber, sourceFile);
    }
}