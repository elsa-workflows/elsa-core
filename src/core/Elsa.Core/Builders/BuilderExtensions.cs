using System;
using System.Runtime.CompilerServices;
using Elsa.Services;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa.Builders
{
    public static class BuilderExtensions
    {
        public static IActivityBuilder Then<T>(
            this IBuilder builder,
            Action<ISetupActivity<T>>? setup,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity => builder.Then(typeof(T).Name, setup, branch, lineNumber, sourceFile);

        public static IActivityBuilder ThenTypeNamed(
            this IBuilder builder,
            string activityTypeName,
            Action<ISetupActivity<IActivity>>? setup,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
            => builder.Then(activityTypeName, setup, branch, lineNumber, sourceFile);

        public static IActivityBuilder Then<T>(
            this IBuilder builder,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) where T : class, IActivity =>
            builder.Then<T>(typeof(T).Name, branch, lineNumber, sourceFile);

        public static IActivityBuilder ThenTypeNamed(
            this IBuilder builder,
            string activityTypeName,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.Then<IActivity>(activityTypeName, branch, lineNumber, sourceFile);
    }
}