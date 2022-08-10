using System;
using Autofac;

namespace Elsa.Extensions
{
    public static class ComponentContextExtensions
    {
        public static T GetRequiredService<T>(this IComponentContext componentContext) where T : class
        {
            return componentContext.Resolve<T>();
        }

        public static T GetRequiredService<T>(this IComponentContext componentContext, Type type) where T : class
        {
            return (T)componentContext.Resolve(type);
        }
    }
}
