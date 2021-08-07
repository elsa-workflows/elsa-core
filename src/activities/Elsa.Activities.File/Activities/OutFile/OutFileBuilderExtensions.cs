using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    public static class OutFileBuilderExtensions
    {
        public static IActivityBuilder OutFile(this IBuilder builder, Action<ISetupActivity<OutFile>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, Func<ActivityExecutionContext, byte[]?> bytes, Func<ActivityExecutionContext, string?> path, Func<ActivityExecutionContext, CopyMode> mode, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity
                    .Set(x => x.Bytes, bytes)
                    .Set(x => x.Path, path)
                    .Set(x => x.Mode, mode), 
                lineNumber, 
                sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<byte[]?>> bytes, Func<ActivityExecutionContext, ValueTask<string?>> path, Func<ActivityExecutionContext, ValueTask<CopyMode>> mode, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity
                    .Set(x => x.Bytes, bytes)
                    .Set(x => x.Path, path)
                    .Set(x => x.Mode, mode),
                lineNumber,
                sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, Func<byte[]?> bytes, Func<string?> path, Func<CopyMode> mode, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity
                    .Set(x => x.Bytes, bytes)
                    .Set(x => x.Path, path)
                    .Set(x => x.Mode, mode),
                lineNumber,
                sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, Func<ValueTask<byte[]?>> bytes, Func<ValueTask<string?>> path, Func<ValueTask<CopyMode>> mode, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity
                    .Set(x => x.Bytes, bytes)
                    .Set(x => x.Path, path)
                    .Set(x => x.Mode, mode),
                lineNumber,
                sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, byte[] bytes, string path, CopyMode mode, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity
                    .Set(x => x.Bytes, bytes)
                    .Set(x => x.Path, path)
                    .Set(x => x.Mode, mode),
                lineNumber,
                sourceFile);
    }
}
