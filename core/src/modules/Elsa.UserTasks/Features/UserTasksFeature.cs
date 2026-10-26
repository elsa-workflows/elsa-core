using Elsa.Common.Serialization;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Extensions;
using Elsa.UserTasks.Extensions;
using Elsa.UserTasks.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.UserTasks.Features;

public sealed class UserTasksFeature(IModule module) : FeatureBase(module)
{
    public Action<Options.UserTasksOptions>? ConfigureOptions { get; set; }

    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<UserTasksFeature>();
        Module.AddActivitiesFrom<UserTasksFeature>();
    }

    public override void Apply()
    {
        Services.AddUserTasksServices(ConfigureOptions);
        Services.Configure<SerializationTypeOptions>(options =>
        {
            options.AddTypeAlias<UserTaskMaterialization>();
        });
        Module.AddFastEndpointsFromModule();
    }
}
