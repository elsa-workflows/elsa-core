using System;
using System.Threading.Tasks;
using Autofac.Multitenant;

namespace Elsa.Multitenancy
{
    public static class MultitenantContainerExtensions
    {
        public static async Task RunInsideInternalScope(this MultitenantContainer container, ITenant tenant, Func<Task> func)
        {
            var strategy = (IOverridableTenantIdentificationStrategy)container.TenantIdentificationStrategy;

            try
            {
                strategy.Tenant = tenant;

                await func();
            }
            finally
            {
                strategy.Tenant = null;
            }
        }

        public static void RunInsideInternalScope(this MultitenantContainer container, ITenant tenant, Action action)
        {
            var strategy = (IOverridableTenantIdentificationStrategy)container.TenantIdentificationStrategy;

            try
            {
                strategy.Tenant = tenant;

                action();
            }
            finally
            {
                strategy.Tenant = null;
            }
        }
    }
}
