using System;

using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Entity
{
    public static class EntityChangedBuilderExtensions
    {
        private static IActivityBuilder EntityChanged(this IBuilder builder, Action<ISetupActivity<EntityChanged>> setup) => builder.Then(setup);
        public static IActivityBuilder EntityChanged(this IBuilder builder, Type entity, EntityChangedAction action) => builder.EntityChanged(setup => setup.WithEntity(entity).WithAction(action));
        public static IActivityBuilder EntityChanged<TEntity>(this IBuilder builder, EntityChangedAction action) => builder.EntityChanged(setup => setup.WithEntity(typeof(TEntity)).WithAction(action));
    }
}
