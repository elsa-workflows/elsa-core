using System;
using System.Linq;
using System.Reflection;
using Autofac;
using MediatR;
using MediatR.Extensions.Autofac.DependencyInjection;
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

        public static IServiceCollection AddNotificationHandlers(this IServiceCollection services, params Type[] markerTypes)
        {
            var assemblies = markerTypes.Select(x => x.GetTypeInfo().Assembly);
            ServiceRegistrar.AddMediatRClasses(services, assemblies, new MediatRServiceConfiguration());
            return services;
        }


        public static ContainerBuilder AddNotificationHandler<T, THandler>(this ContainerBuilder containerBuilder)
            where T : INotification
            where THandler : INotificationHandler<T>
        {
            containerBuilder.RegisterType<INotificationHandler<T>>().As<THandler>().InstancePerDependency();
            return containerBuilder;
        }

        public static ContainerBuilder AddNotificationHandlersFrom<TMarker>(this ContainerBuilder containerBuilder) => containerBuilder.AddNotificationHandlers(typeof(TMarker));

        public static ContainerBuilder AddNotificationHandlers(this ContainerBuilder containerBuilder, params Type[] markerTypes)
        {
            var assemblies = markerTypes.Select(x => x.GetTypeInfo().Assembly);

            containerBuilder.RegisterMediatR(assemblies);

            return containerBuilder;
        }
    }
}