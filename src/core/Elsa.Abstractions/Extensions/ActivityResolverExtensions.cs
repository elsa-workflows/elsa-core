using System;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions
{
    public static class ActivityResolverExtensions
    {
        public static T ResolveActivity<T>(this IActivityResolver resolver, Action<T> configure = null) where T : IActivity
        {
            var activity = (T) resolver.ResolveActivity(typeof(T).Name);

            configure?.Invoke(activity);
            return activity;
        }
    }
}