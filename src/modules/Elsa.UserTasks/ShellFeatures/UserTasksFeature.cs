using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.ShellFeatures;
using Elsa.UserTasks.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.UserTasks.ShellFeatures;

[ShellFeature(
    DisplayName = "User Tasks",
    Description = "Provides identity-neutral, workflow-bound human tasks.",
    DependsOn = [typeof(ElsaFastEndpointsFeature)])]
[UsedImplicitly]
public sealed class UserTasksFeature : IFastEndpointsShellFeature
{
    public string DefaultTenantId { get; set; } = "";
    public string DefaultProvider { get; set; } = "default";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddUserTasksServices(options =>
        {
            options.DefaultTenantId = DefaultTenantId;
            options.DefaultProvider = DefaultProvider;
        });
    }
}
