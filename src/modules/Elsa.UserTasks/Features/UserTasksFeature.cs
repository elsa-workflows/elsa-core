using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Elsa.UserTasks.Extensions;

namespace Elsa.UserTasks.Features;

public sealed class UserTasksFeature(IModule module) : FeatureBase(module)
{
    public Action<Options.UserTasksOptions>? ConfigureOptions { get; set; }

    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<UserTasksFeature>();
    }

    public override void Apply()
    {
        Services.AddUserTasksServices(ConfigureOptions);
        Module.AddFastEndpointsFromModule();
    }
}
