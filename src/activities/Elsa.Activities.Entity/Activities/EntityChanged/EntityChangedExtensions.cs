using System;

using Elsa.Activities.Entity.Extensions;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Entity
{
    public static class EntityChangedExtensions
    {
        public static ISetupActivity<EntityChanged> WithEntity<T>(this ISetupActivity<EntityChanged> activity) => activity.WithEntity(typeof(T));
        public static ISetupActivity<EntityChanged> WithEntity(this ISetupActivity<EntityChanged> activity, Type type) => activity.Set(x => x.EntityName, type.GetEntityName(false));
        public static ISetupActivity<EntityChanged> WithAction(this ISetupActivity<EntityChanged> activity, EntityChangedAction action) => activity.Set(x => x.Action, action);
    }
}
