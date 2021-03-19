using System;
using System.Runtime.CompilerServices;
using Elsa.Builders;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.JavaScript
{
    public static class RunJavaScriptBuilderExtensions
    {
        public static IActivityBuilder RunJavaScript(this IBuilder builder, Action<ISetupActivity<RunJavaScript>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder RunJavaScript(this IBuilder builder, string script, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.RunJavaScript(setup => setup.WithScript(script), lineNumber, sourceFile);
    }
}