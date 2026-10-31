using Elsa.Common.Multitenancy;
using Elsa.Features.Services;
using Elsa.Workflows.Admission.Persistence.EFCore.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Workflows.Admission.Persistence.EFCore.Extensions;

public static class AdmissionPersistenceExtensions
{
    public static IModule UseAdmissionPersistence(this IModule module, Action<EFCoreAdmissionPersistenceFeature> configure)
    {
        module.Configure(configure);
        return module;
    }

    internal static void AddAdmissionPersistence(this IServiceCollection services, AdmissionPersistenceScope scope)
    {
        // The shared factory needs an accessor even in a single-scope host. Preserve
        // any accessor supplied by the host; scope is validated by each feature.
        services.TryAddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        services.AddSingleton(scope);
        services.AddScoped<IAdmissionStore, EFCoreAdmissionStore>();
    }
}
