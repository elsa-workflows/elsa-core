using System;
using Autofac;
using Elsa.Extensions;
using Elsa.Services;

namespace Elsa.Runtime
{
    public static class ContainerBuilderExtensions
    {
        public static ContainerBuilder AddStartupRunner(this ContainerBuilder services)
        {
            services.RegisterType<StartupRunner>().As<IStartable>().InstancePerLifetimeScope();

            return services;
        }

        public static ContainerBuilder AddStartupTask<TStartupTask>(this ContainerBuilder services) where TStartupTask : class, IStartupTask
        {
            return services
                .AddScoped<TStartupTask>()
                .AddScoped<IStartupTask, TStartupTask>(sp => sp.GetRequiredService<TStartupTask>());
        }

        public static ContainerBuilder AddStartupTask<TStartupTask>(this ContainerBuilder services, Func<IServiceProvider, TStartupTask> factory) where TStartupTask : class, IStartupTask
        {
            return services
                .AddScoped(factory)
                .AddScoped<IStartupTask, TStartupTask>(sp => sp.GetRequiredService<TStartupTask>());
        }
    }
}
