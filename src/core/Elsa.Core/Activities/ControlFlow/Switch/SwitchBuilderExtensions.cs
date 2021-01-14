using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class SwitchBuilderExtensions
    {
        public static IActivityBuilder Switch(
            this IBuilder builder,
            Action<ISetupActivity<Switch>>? setup = default,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
        {
            var activityBuilder = builder.Then(setup, null, lineNumber, sourceFile);
            activity?.Invoke(activityBuilder);
            return activityBuilder;
        }

        public static IActivityBuilder Switch(
            this IBuilder builder,
            Func<ActivityExecutionContext, ValueTask<ICollection<SwitchCase>>> cases,
            SwitchMode mode,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.Switch(x => x.WithCases(cases).WithMode(mode), activity, lineNumber, sourceFile);

        public static IActivityBuilder Switch(
            this IBuilder builder,
            Func<ActivityExecutionContext, ValueTask<ICollection<SwitchCase>>> cases,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.Switch(x => x.WithCases(cases), activity, lineNumber, sourceFile);

        public static IActivityBuilder Switch(
            this IBuilder builder,
            Func<ActivityExecutionContext, ICollection<SwitchCase>> cases,
            SwitchMode mode,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.Switch(x => x.WithCases(cases).WithMode(mode), activity, lineNumber, sourceFile);

        public static IActivityBuilder Switch(
            this IBuilder builder,
            Func<ActivityExecutionContext, ICollection<SwitchCase>> cases,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.Switch(x => x.WithCases(cases), activity, lineNumber, sourceFile);

        public static IActivityBuilder Switch(
            this IBuilder builder,
            Func<ICollection<SwitchCase>> cases,
            SwitchMode mode,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.Switch(x => x.WithCases(cases).WithMode(mode), activity, lineNumber, sourceFile);

        public static IActivityBuilder Switch(
            this IBuilder builder,
            Func<ICollection<SwitchCase>> cases,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.Switch(x => x.WithCases(cases), activity, lineNumber, sourceFile);

        public static IActivityBuilder Switch(
            this IBuilder builder,
            ICollection<SwitchCase> cases,
            SwitchMode mode,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.Switch(x => x.WithCases(cases).WithMode(mode), activity, lineNumber, sourceFile);

        public static IActivityBuilder Switch(this IBuilder builder, ICollection<SwitchCase> cases, Action<IActivityBuilder>? activity = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Switch(x => x.WithCases(cases), activity, lineNumber, sourceFile);

        public static IActivityBuilder
            Switch(this IBuilder builder, Action<SwitchCaseBuilder> cases, Action<IActivityBuilder>? activity = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Switch(cases, SwitchMode.MatchFirst, activity, lineNumber, sourceFile);

        public static IActivityBuilder Switch(
            this IBuilder builder,
            Action<SwitchCaseBuilder> cases,
            SwitchMode mode,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
        {
            return builder.Switch(x =>
            {
                cases(x);
                return new ValueTask();
            }, mode, activity, lineNumber, sourceFile);
        }

        public static IActivityBuilder Switch(
            this IBuilder builder,
            Func<SwitchCaseBuilder, ValueTask> cases,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.Switch(cases, SwitchMode.MatchFirst, activity, lineNumber, sourceFile);

        public static IActivityBuilder Switch(
            this IBuilder builder,
            Func<SwitchCaseBuilder, ValueTask> cases,
            SwitchMode mode,
            Action<IActivityBuilder>? activity = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
        {
            return builder.Switch(async context =>
            {
                var switchCaseBuilder = new SwitchCaseBuilder(context);
                await cases(switchCaseBuilder);
                return switchCaseBuilder.Cases;
            }, mode, activity, lineNumber, sourceFile);
        }
    }
}