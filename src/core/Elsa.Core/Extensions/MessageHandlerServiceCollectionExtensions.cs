using System;
using System.Linq;
using System.Reflection;
using MediatR;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions
{
    public static class MessageHandlerServiceCollectionExtensions
    {
        public static IServiceCollection AddNotificationHandler<T, THandler>(this IServiceCollection services)
            where T : INotification
            where THandler : INotificationHandler<T>
        {
            return services.AddTransient(typeof(INotificationHandler<T>), typeof(THandler));
        }
        
        public static IServiceCollection AddNotificationHandlers(this IServiceCollection services, params Type[] markerTypes)
        {
            var assemblies = markerTypes.Select(x => x.GetTypeInfo().Assembly);
            ServiceRegistrar.AddMediatRClasses(services, assemblies);
            return services;
        }
    }
}