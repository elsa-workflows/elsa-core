using System;
using System.Runtime.CompilerServices;
using Elsa.Services;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa.Builders
{
    public static class ActivityBuilderExtensions
    {
        public static IActivityBuilder Add<T>(
            this IActivityBuilder activityBuilder,
            Action<ISetupActivity<T>>? setup = default,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity =>
            activityBuilder.Add(typeof(T).Name, setup, branch, lineNumber, sourceFile);
    }
}