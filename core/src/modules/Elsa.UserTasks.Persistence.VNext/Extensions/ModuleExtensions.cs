using Elsa.Features.Services;

namespace Elsa.UserTasks.Persistence.VNext.Extensions;

public static class ModuleExtensions
{
    public static IModule UseUserTasksPersistenceVNext(this IModule module)
    {
        module.Services.AddUserTasksPersistenceVNext();
        return module;
    }
}
