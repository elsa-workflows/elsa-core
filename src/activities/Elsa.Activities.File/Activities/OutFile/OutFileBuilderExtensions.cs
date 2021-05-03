using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.File
{
    public static class OutFileBuilderExtensions
    {
        public static IActivityBuilder OutFile(this IBuilder builder, Action<ISetupActivity<OutFile>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, Func<ActivityExecutionContext, string> content, Func<ActivityExecutionContext, string> path, Func<ActivityExecutionContext, CopyMode> mode, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity
                    .Set(x => x.Content, content)
                    .Set(x => x.Path, path)
                    .Set(x => x.Mode, mode), 
                lineNumber, 
                sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, Func<ActivityExecutionContext, ValueTask<string>> content, Func<ActivityExecutionContext, ValueTask<string>> path, Func<ActivityExecutionContext, ValueTask<CopyMode>> mode, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity
                    .Set(x => x.Content, content)
                    .Set(x => x.Path, path)
                    .Set(x => x.Mode, mode),
                lineNumber,
                sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, Func<string> content, Func<string> path, Func<CopyMode> mode, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity
                    .Set(x => x.Content, content)
                    .Set(x => x.Path, path)
                    .Set(x => x.Mode, mode),
                lineNumber,
                sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, Func<ValueTask<string>> content, Func<ValueTask<string>> path, Func<ValueTask<CopyMode>> mode, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity
                    .Set(x => x.Content, content)
                    .Set(x => x.Path, path)
                    .Set(x => x.Mode, mode),
                lineNumber,
                sourceFile);

        public static IActivityBuilder OutFile(this IBuilder builder, string content, string path, CopyMode mode, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.OutFile(activity => activity
                    .Set(x => x.Content, content)
                    .Set(x => x.Path, path)
                    .Set(x => x.Mode, mode),
                lineNumber,
                sourceFile);
    }
}
