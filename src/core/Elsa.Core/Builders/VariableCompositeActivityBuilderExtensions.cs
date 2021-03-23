using System;
using System.Runtime.CompilerServices;
using Elsa.Activities.Primitives;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa.Builders
{
    public static class VariableCompositeActivityBuilderExtensions
    {
        public static IActivityBuilder SetVariable(this ICompositeActivityBuilder builder, string variableName, object? value, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.StartWith<SetVariable>(
                activity =>
                {
                    activity.Set(x => x.VariableName, _ => variableName);
                    activity.Set(x => x.Value, _ => value);
                }, null, lineNumber, sourceFile);

        public static IActivityBuilder SetVariable(
            this ICompositeActivityBuilder builder,
            string variableName,
            Func<object?> value,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.SetVariable(variableName, value(), lineNumber, sourceFile);
    }
}