using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Menu;
using Elsa.Studio.Security.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Security.Extensions;

/// <summary>
/// Provides extension methods for configuring security services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the security module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="backendApiConfig">Optional backend configuration used to register Identity API clients.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddSecurityModule(this IServiceCollection services, BackendApiConfig? backendApiConfig = null)
    {
        services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, SecurityMenu>()
            .AddScoped<ISecurityMenuContributor, IdentitySecurityMenuContributor>()
            .AddScoped<IIdentityPermissionContext, IdentityPermissionContext>()
            .AddScoped<IUserAdministrationAccessService, UserAdministrationAccessService>()
            .AddScoped<IRoleAdministrationAccessService, RoleAdministrationAccessService>()
            .AddScoped<IRoleDeletionService, RoleDeletionService>()
            .AddClipboardInterop()
            .AddRemoteApi<IUsersApi>(backendApiConfig)
            .AddRemoteApi<IRolesApi>(backendApiConfig)
            .AddRemoteApi<IPermissionsApi>(backendApiConfig)
            .AddRemoteApi<IMePermissionsApi>(backendApiConfig);

        return services;
    }
}
