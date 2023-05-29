using System;
using System.Linq;
using System.Reflection;
using MediatR;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa
{
    public static class MessageHandlerServiceCollectionExtensions
    {
        public static IServiceCollection AddNotificationHandler<T, THandler>(this IServiceCollection services)
            where T : INotification
            where THandler : INotificationHandler<T>
        {
            return services.AddTransient(typeof(INotificationHandler<T>), typeof(THandler));
        }

        public static IServiceCollection AddNotificationHandlersFrom<TMarker>(this IServiceCollection services) => services.AddNotificationHandlers(typeof(TMarker));
        
        public static IServiceCollection AddNotificationHandlers(this IServiceCollection services, params Type[] markerTypes)
        {
            var assemblies = markerTypes.Select(x => x.GetTypeInfo().Assembly).ToArray();
            var serviceConfiguration = new MediatRServiceConfiguration();
            serviceConfiguration.RegisterServicesFromAssemblies(assemblies);
            ServiceRegistrar.AddMediatRClasses(services, serviceConfiguration);
            return services;
        }
    }
}