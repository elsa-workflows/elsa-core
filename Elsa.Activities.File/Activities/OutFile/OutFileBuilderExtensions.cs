using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.File
{
    public static class OutFileBuilderExtensions
    {
        public static IActivityBuilder OutFile(this IBuilder builder, Action<ISetupActivity<OutFile>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, Func<ActivityExecutionContext, string> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity.WithPath(path), lineNumber, sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string>> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity.WithPath(path!), lineNumber, sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, Func<string> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity.WithPath(path!), lineNumber, sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, Func<ValueTask<string>> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity.WithPath(path!), lineNumber, sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, string path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity.WithPath(path!), lineNumber, sourceFile);
    }
}
