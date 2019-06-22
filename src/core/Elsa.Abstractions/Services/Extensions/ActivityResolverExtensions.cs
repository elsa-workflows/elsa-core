using System;
using Elsa.Services.Models;

namespace Elsa.Services.Extensions
{
    public static class ActivityResolverExtensions
    {
        public static T ResolveActivity<T>(this IActivityResolver resolver, Action<T> configure = null) where T : IActivity
        {
            var activity = (T) resolver.ResolveActivity(typeof(T).Name);

            if (activity == null)
                activity = Activator.CreateInstance<T>();
            
            configure?.Invoke(activity);
            return activity;
        }
    }
}