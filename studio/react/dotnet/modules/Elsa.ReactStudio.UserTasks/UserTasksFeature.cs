using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.UserTasks;

/// <summary>Contributes the independently built UserTasks Preview module when enabled.</summary>
[ShellFeature("ReactStudio.UserTasks", DependsOn = [typeof(ReactStudioPreviewFeature)])]
public sealed class UserTasksFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("user-tasks", "UserTasks", "Elsa.ReactStudio.UserTasks", false,
            typeof(UserTasksFeature).Assembly, ["Elsa.UserTasks"]));
    }
}
