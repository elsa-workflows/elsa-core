using Elsa.Common.Entities;
using Elsa.Tenants.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Tenants.Services;

public class DefaultShellFactory(IApplicationServicesAccessor applicationServicesAccessor) : IShellFactory
{
    public Shell CreateShell(Tenant tenant)
    {
        var applicationServices = applicationServicesAccessor.ApplicationServices;
        var tenantServiceCollection = new ServiceCollection();
        tenantServiceCollection.AddSingleton(tenant);

        foreach (var serviceDescriptor in applicationServices)
        {
            tenantServiceCollection.Add(serviceDescriptor);
        }

        var tenantServiceProvider = tenantServiceCollection.BuildServiceProvider();

        return new Shell(tenantServiceCollection, tenantServiceProvider);
    }
}