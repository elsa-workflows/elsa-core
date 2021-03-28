using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForEachBuilderExtensions
    {
        public static IActivityBuilder ForEach(
            this IBuilder builder,
            Action<ISetupActivity<ForEach>> setup,
            Action<IOutcomeBuilder> iterate,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, branch => iterate(branch.When(OutcomeNames.Iterate)), lineNumber, sourceFile);

        public static IActivityBuilder ForEach<T>(
            this IBuilder builder,
            Func<ActivityExecutionContext, ICollection<T>> items,
            Action<IOutcomeBuilder> iterate,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.ForEach(activity => activity.Set(x => x.Items, context => items(context).Select(x => (object)x!).ToList()), iterate, lineNumber, sourceFile);

        public static IActivityBuilder ForEach<T>(
            this IBuilder builder,
            Func<ICollection<T>> items,
            Action<IOutcomeBuilder> iterate,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.ForEach(activity => activity.Set(x => x.Items, () => items().Select(x => (object)x!).ToList()), iterate, lineNumber, sourceFile);

        public static IActivityBuilder ForEach<T>(
            this IBuilder builder,
            ICollection<T> items,
            Action<IOutcomeBuilder> iterate,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) =>
            builder.ForEach(activity => activity.Set(x => x.Items, items.Select(x => (object)x!).ToList()), iterate, lineNumber, sourceFile);
    }
}