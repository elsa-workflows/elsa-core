using System;
using System.Runtime.CompilerServices;
using Elsa.Builders;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Entity
{
    public static class EntityChangedBuilderExtensions
    {
        private static IActivityBuilder EntityChanged(this IBuilder builder, Action<ISetupActivity<EntityChanged>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder EntityChanged(this IBuilder builder, Type entity, EntityChangedAction action, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.EntityChanged(setup => setup.WithEntity(entity).WithAction(action), lineNumber, sourceFile);

        public static IActivityBuilder EntityChanged<TEntity>(this IBuilder builder, EntityChangedAction action, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.EntityChanged(setup => setup.WithEntity(typeof(TEntity)).WithAction(action), lineNumber, sourceFile);
    }
}