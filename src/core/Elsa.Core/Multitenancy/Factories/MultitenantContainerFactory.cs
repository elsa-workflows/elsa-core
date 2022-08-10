using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Multitenant;
using Elsa.Options;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Multitenancy
{
    public static class MultitenantContainerFactory
    {
        public static MultitenantContainer CreateSampleMultitenantContainer(IServiceCollection services, Action<ElsaOptionsBuilder>? configureOptions = null, Action<ContainerBuilder>? configureServices = null)
        {
            var containerBuilder = new ContainerBuilder();

            configureServices?.Invoke(containerBuilder);

            containerBuilder.ConfigureElsaServices(services, configureOptions);

            containerBuilder.Populate(services);

            var container = containerBuilder.Build();

            var strategy = TenantIdentificationStrategyFactory.CreateSampleStrategy(out var tenant);

            var mtc = new MultitenantContainer(strategy, container);

            mtc.ConfigureTenant(tenant.Id, cb =>
            {
                mtc.RunInsideInternalScope(tenant, () =>
                {
                    cb.RegisterInstance(tenant).AsImplementedInterfaces();

                    cb.AddStartupRunner();
                });
            });

            return mtc;
        }

        public static MultitenantContainer CreateSampleMultitenantContainer(IContainer container)
        {
            var strategy = TenantIdentificationStrategyFactory.CreateSampleStrategy(out var tenant);

            var mtc = new MultitenantContainer(strategy, container);

            mtc.ConfigureTenant(tenant.Id, cb =>
            {
                mtc.RunInsideInternalScope(tenant, () =>
                {
                    cb.RegisterInstance(tenant).AsImplementedInterfaces();

                    cb.AddStartupRunner();
                });
            });

            return mtc;
        }

        public static MultitenantContainer CreateMultitenantContainer(IContainer container)
        {
            var strategy = TenantIdentificationStrategyFactory.CreateStrategy(container);
            var mtc = new MultitenantContainer(strategy, container);

            var tenantStore = container.Resolve<ITenantStore>();
            var tenants = tenantStore.GetTenantsAsync().GetAwaiter().GetResult();

            foreach (var tenant in tenants)
            {
                mtc.ConfigureTenant(tenant.Id, cb =>
                {
                    mtc.RunInsideInternalScope(tenant, () =>
                    {
                        cb.RegisterInstance(tenant).AsImplementedInterfaces();

                        cb.AddStartupRunner();
                    });
                });
            }

            return mtc;
        }
    }
}
