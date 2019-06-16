using System;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions
{
    public static class ServiceProviderExtensions
    {
        public static T CreateActivity<T>(this IServiceProvider serviceProvider) where T : IActivity
        {
            var idGenerator = serviceProvider.GetRequiredService<IIdGenerator>();
            var activity = serviceProvider.GetRequiredService<T>();

            activity.Id = idGenerator.Generate();
            return activity;
        }
    }
}