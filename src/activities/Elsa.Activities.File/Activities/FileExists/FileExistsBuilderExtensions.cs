using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    public static class FileExistsBuilderExtensions
    {
        public static IActivityBuilder FileExists(this IActivityBuilder builder, Action<ISetupActivity<FileExists>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder FileExists(this IActivityBuilder builder, Func<ActivityExecutionContext, string?> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.FileExists(activity => activity
                    .Set(x => x.Path, path),
                lineNumber,
                sourceFile);

        public static IActivityBuilder FileExists(this IActivityBuilder builder, Func<ActivityExecutionContext, ValueTask<string?>> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.FileExists(activity => activity
                    .Set(x => x.Path, path),
                lineNumber,
                sourceFile);

        public static IActivityBuilder FileExists(this IActivityBuilder builder, Func<string?> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.FileExists(activity => activity
                    .Set(x => x.Path, path),
                lineNumber,
                sourceFile);

        public static IActivityBuilder FileExists(this IActivityBuilder builder, Func<ValueTask<string?>> path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.FileExists(activity => activity
                    .Set(x => x.Path, path),
                lineNumber,
                sourceFile);

        public static IActivityBuilder FileExists(this IActivityBuilder builder, string path, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.FileExists(activity => activity
                    .Set(x => x.Path, path),
                lineNumber,
                sourceFile);
    }
}