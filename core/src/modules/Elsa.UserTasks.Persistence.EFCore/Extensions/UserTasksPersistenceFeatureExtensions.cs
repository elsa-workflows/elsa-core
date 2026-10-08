using Elsa.UserTasks.Features;
using Elsa.UserTasks.Persistence.EFCore.Features;

namespace Elsa.UserTasks.Persistence.EFCore.Extensions;

public static class UserTasksPersistenceFeatureExtensions
{
    public static UserTasksFeature UseEntityFrameworkCore(this UserTasksFeature feature, Action<EFCoreUserTasksPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}
