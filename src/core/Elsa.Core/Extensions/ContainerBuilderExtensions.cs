using Autofac;
using Autofac.Multitenant;
using Elsa.HostedServices;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder AddHostedService<T>(this ContainerBuilder containerBuilder) where T : class, IElsaHostedService
        {
            containerBuilder.AddScoped<T>();
            containerBuilder.AddScoped<IElsaHostedService, T>(cc => cc.Resolve<T>());

            return containerBuilder;
        }

        /// <summary>
        /// Registers the specified service only if none already exists for the specified provider type.
        /// </summary>
        public static ContainerBuilder TryAddProvider<TService, TProvider>(
            this ContainerBuilder containerBuilder,
            ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    return containerBuilder.TryAddTransientProvider<TService, TProvider>();
                case ServiceLifetime.Singleton:
                    return containerBuilder.TryAddMultitonProvider<TService, TProvider>(); ;
                case ServiceLifetime.Scoped:
                default:
                    return containerBuilder.TryAddScopedProvider<TService, TProvider>(); ;
            }
        }

        private static ContainerBuilder TryAddTransientProvider<TService, TProvider>(
            this ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType(typeof(TProvider)).As(typeof(TService)).InstancePerDependency();
            return containerBuilder;
        }

        public static ContainerBuilder TryAddScopedProvider<TService, TProvider>(
            this ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType(typeof(TProvider)).As(typeof(TService)).InstancePerLifetimeScope();
            return containerBuilder;
        }

        public static ContainerBuilder TryAddMultitonProvider<TService, TProvider>(
            this ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType(typeof(TProvider)).As(typeof(TService)).InstancePerTenant();
            return containerBuilder;
        }
    }
}
