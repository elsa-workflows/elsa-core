using Elsa.Features.Services;
using Elsa.Workflows.Admission.Persistence.EFCore.Features;

namespace Elsa.Workflows.Admission.Persistence.EFCore.Extensions;

public static class AdmissionPersistenceExtensions
{
    public static IModule UseAdmissionPersistence(this IModule module, Action<EFCoreAdmissionPersistenceFeature> configure)
    {
        module.Configure(configure);
        return module;
    }
}
