using Elsa.Features.Services;

namespace Elsa.Secrets.Persistence.VNext.Extensions;

public static class ModuleExtensions
{
    public static IModule UseSecretsPersistenceVNext(this IModule module)
    {
        module.Services.AddSecretsPersistenceVNext();
        return module;
    }
}
