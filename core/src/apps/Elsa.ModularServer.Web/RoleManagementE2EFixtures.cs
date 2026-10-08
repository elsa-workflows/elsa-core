using Elsa.Permissions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ModularServer.Web;

/// <summary>Opt-in services used by the Elsa Studio role-management acceptance harness.</summary>
public static class RoleManagementE2EFixtureServiceCollectionExtensions
{
    /// <summary>Configuration switch that enables the role-management catalog fixture.</summary>
    public const string IncludeUnverifiedPermissionDescriptorKey = "RoleManagementE2EFixtures:IncludeUnverifiedPermissionDescriptor";

    /// <summary>
    /// Adds the deterministic role-management fixture when explicitly enabled by the host configuration.
    /// The default path is intentionally a no-op so ordinary sample and production hosts do not change.
    /// </summary>
    public static IServiceCollection AddRoleManagementE2EFixtures(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration.GetValue<bool>(IncludeUnverifiedPermissionDescriptorKey))
            services.AddSingleton<IPermissionDescriptorProvider>(RoleManagementE2EFixturePermissionDescriptorProvider.Instance);

        return services;
    }
}

/// <summary>Provides the one unverified descriptor needed by the role-management acceptance harness.</summary>
/// <remarks>
/// This is a value type on purpose: the shell descriptor scanner discovers class-based module providers, while
/// this provider is registered only through the explicit opt-in above. It therefore cannot leak into the normal
/// catalog merely because the host assembly is loaded.
/// </remarks>
internal readonly struct RoleManagementE2EFixturePermissionDescriptorProvider : IPermissionDescriptorProvider
{
    public static RoleManagementE2EFixturePermissionDescriptorProvider Instance => new();

    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(
            "e2e/role-management/unverified",
            [Authorization.CoreVerbs.View],
            "E2E unverified permission",
            "Fixture-only permission descriptor for role-management acceptance testing.",
            "E2E fixtures",
            Verified: false)
    ];
}
