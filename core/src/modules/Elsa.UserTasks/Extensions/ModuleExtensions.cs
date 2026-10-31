using Elsa.Features.Services;
using Elsa.UserTasks.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class UserTasksModuleExtensions
{
    public static IModule UseUserTasks(this IModule module, Action<UserTasksFeature>? configure = null) => module.Use(configure);
}
