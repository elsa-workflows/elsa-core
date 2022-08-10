using System;
using Autofac;
using Autofac.Multitenant;

namespace Elsa.Extensions
{
    // these extension methods are just a syntactic sugar for easier transition from MS IoC to Autofac
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder AddTransient(this ContainerBuilder containerBuilder, Type type)
        {
            containerBuilder.RegisterType(type).InstancePerDependency();
            return containerBuilder;
        }

        public static ContainerBuilder AddTransient(this ContainerBuilder containerBuilder, Type serviceType, Type implementationType)
        {
            containerBuilder.RegisterType(implementationType).As(serviceType).InstancePerDependency();
            return containerBuilder;
        }

        public static ContainerBuilder AddTransient<TService, TImplementation>(this ContainerBuilder containerBuilder) where TImplementation : class, TService where TService : class
        {
            containerBuilder.RegisterType<TImplementation>().As<TService>().InstancePerDependency();
            return containerBuilder;
        }

        public static ContainerBuilder AddTransient<TImplementation>(this ContainerBuilder containerBuilder, Func<IServiceProvider, TImplementation> implementationFactory) where TImplementation : class
        {
            containerBuilder.Register(cc =>
            {
                var sp = cc.Resolve<IServiceProvider>();
                return implementationFactory(sp);
            }).InstancePerDependency();
            return containerBuilder;
        }

        public static ContainerBuilder AddTransient<TImplementation>(this ContainerBuilder containerBuilder) where TImplementation : class
        {
            containerBuilder.RegisterType<TImplementation>().InstancePerDependency();
            return containerBuilder;
        }

        public static ContainerBuilder AddScoped<TImplementation>(this ContainerBuilder containerBuilder) where TImplementation : class
        {
            containerBuilder.RegisterType<TImplementation>().InstancePerLifetimeScope();
            return containerBuilder;
        }

        public static ContainerBuilder AddScoped<TService, TImplementation>(this ContainerBuilder containerBuilder) where TImplementation : class, TService where TService : class
        {
            containerBuilder.RegisterType<TImplementation>().As<TService>().InstancePerLifetimeScope();
            return containerBuilder;
        }

        public static ContainerBuilder AddScoped<TService, TImplementation>(this ContainerBuilder containerBuilder, Func<IComponentContext, TImplementation> implementationFactory) where TImplementation : class, TService where TService : class
        {
            containerBuilder.Register(implementationFactory).As<TService>().InstancePerLifetimeScope();
            return containerBuilder;
        }

        public static ContainerBuilder AddScoped<TImplementation>(this ContainerBuilder containerBuilder, Func<IServiceProvider, TImplementation> implementationFactory) where TImplementation : class
        {
            containerBuilder.Register(cc =>
            {
                var sp = cc.Resolve<IServiceProvider>();
                return implementationFactory(sp);

            }).InstancePerLifetimeScope();
            return containerBuilder;
        }

        public static ContainerBuilder AddMultiton<TService, TImplementation>(this ContainerBuilder containerBuilder) where TImplementation : class, TService where TService : class
        {
            containerBuilder.RegisterType<TImplementation>().As<TService>().InstancePerTenant();
            return containerBuilder;
        }

        public static ContainerBuilder AddMultiton<TImplementation>(this ContainerBuilder containerBuilder) where TImplementation : class
        {
            containerBuilder.RegisterType<TImplementation>().InstancePerTenant();
            return containerBuilder;
        }

        public static ContainerBuilder AddMultiton<TImplementation>(this ContainerBuilder containerBuilder, Func<IServiceProvider, TImplementation> implementationFactory) where TImplementation : class
        {
            containerBuilder.Register(cc =>
            {
                var sp = cc.Resolve<IServiceProvider>();
                return implementationFactory(sp);

            }).InstancePerTenant();
            return containerBuilder;
        }

        public static ContainerBuilder AddMultiton<TService, TImplementation>(this ContainerBuilder containerBuilder, Func<IComponentContext, TImplementation> implementationFactory) where TImplementation : class, TService where TService : class
        {
            containerBuilder.Register(implementationFactory).As<TService>().InstancePerTenant();
            return containerBuilder;
        }

        public static ContainerBuilder AddMultiton<TImplementation>(this ContainerBuilder containerBuilder, TImplementation instance) where TImplementation : class
        {
            containerBuilder.Register(_ => instance).InstancePerTenant();
            return containerBuilder;
        }

        public static ContainerBuilder AddSingleton<TImplementation>(this ContainerBuilder containerBuilder, TImplementation instance) where TImplementation : class
        {
            containerBuilder.RegisterInstance(instance);
            return containerBuilder;
        }

        public static ContainerBuilder AddSingleton<TService, TImplementation>(this ContainerBuilder containerBuilder) where TImplementation : class, TService where TService : class
        {
            containerBuilder.RegisterType<TImplementation>().As<TService>().SingleInstance();
            return containerBuilder;
        }

        public static ContainerBuilder Decorate<TService, TImplementation>(this ContainerBuilder containerBuilder) where TImplementation : class, TService where TService : class
        {
            containerBuilder.RegisterDecorator<TImplementation, TService>();
            return containerBuilder;
        }
    }
}
