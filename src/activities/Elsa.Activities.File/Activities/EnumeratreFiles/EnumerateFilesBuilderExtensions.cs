using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    public static class EnumerateFilesBuilderExtensions
    {
        public static IActivityBuilder EnumerateFiles(this IBuilder builder, Action<ISetupActivity<EnumerateFiles>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder EnumerateFiles(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string?>> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.EnumerateFiles(activity => activity
                    .Set(x => x.Path, path),
                lineNumber,
                sourceFile);

        public static IActivityBuilder EnumerateFiles(this IBuilder builder, Func<ActivityExecutionContext, string> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.EnumerateFiles(activity => activity
                    .Set(x => x.Path, path),
                lineNumber,
                sourceFile);

        public static IActivityBuilder EnumerateFiles(this IBuilder builder, Func<ValueTask<string?>> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.EnumerateFiles(activity => activity
                    .Set(x => x.Path, path),
                lineNumber,
                sourceFile);

        public static IActivityBuilder EnumerateFiles(this IBuilder builder, Func<string> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.EnumerateFiles(activity => activity
                    .Set(x => x.Path, path),
                lineNumber,
                sourceFile);

        public static IActivityBuilder EnumerateFiles(this IBuilder builder, string path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.EnumerateFiles(activity => activity
                    .Set(x => x.Path, path),
                lineNumber,
                sourceFile);
    }
}
