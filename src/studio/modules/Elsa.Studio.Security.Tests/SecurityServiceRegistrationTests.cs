using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Extensions;
using Elsa.Studio.Security.Menu;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class SecurityServiceRegistrationTests
{
    [Fact]
    public void AddSecurityModuleWithoutConfigurationKeepsTheLocalFeatureAndMenuRegistration()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.AddSecurityModule());
        Assert.Equal(ServiceLifetime.Scoped, FindDescriptor<IFeature>(services).Lifetime);
        Assert.Equal(typeof(Feature), FindDescriptor<IFeature>(services).ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, FindDescriptor<IMenuProvider>(services).Lifetime);
        Assert.Equal(typeof(SecurityMenu), FindDescriptor<IMenuProvider>(services).ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, FindDescriptor<IIdentityPermissionContext>(services).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, FindDescriptor<IUserAdministrationAccessService>(services).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, FindDescriptor<IRoleAdministrationAccessService>(services).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, FindDescriptor<IRoleDeletionService>(services).Lifetime);
    }

    [Fact]
    public void AddSecurityModuleNoLongerRegistersTheLegacyClaimBasedPermissionService()
    {
        var services = new ServiceCollection();

        services.AddSecurityModule();

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType.Name == "IIdentityPermissionService");
    }

    [Fact]
    public void AddSecurityModuleRegistersApiFactoriesUsedByTheSharedBackendProvider()
    {
        var services = new ServiceCollection();

        services.AddSecurityModule(new BackendApiConfig());

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IUsersApi));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRolesApi));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IPermissionsApi));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IMePermissionsApi));
    }

    [Fact]
    public void BackendConfigurationParameterRemainsOptionalForExistingCallers()
    {
        var parameter = typeof(ServiceCollectionExtensions)
            .GetMethod(nameof(ServiceCollectionExtensions.AddSecurityModule))!
            .GetParameters()[1];

        Assert.True(parameter.IsOptional);
        Assert.Null(parameter.DefaultValue);
    }

    private static ServiceDescriptor FindDescriptor<T>(IServiceCollection services) =>
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(T));
}
