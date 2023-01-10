using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Services;
using Elsa.Services.Models;
using static Elsa.Builders.RunInlineHelpers;

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
        
        public static IActivityBuilder Add(
            this IActivityBuilder activityBuilder,
            Func<ActivityExecutionContext, ValueTask> activity,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            activityBuilder.Add<Inline>(inline => inline.Set(x => x.Function, RunInline(activity)), branch, lineNumber, sourceFile);
        
        
    }
}