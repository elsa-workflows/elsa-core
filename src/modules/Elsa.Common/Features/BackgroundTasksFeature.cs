using Elsa.Common.Multitenancy.HostedServices;
using Elsa.Common.RecurringTasks;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Features;

public class BackgroundTasksFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        Services.AddSingleton<RecurringTaskScheduleManager>();
        Services.AddScoped<TaskExecutor>();
        Services.AddStartupTask<ConfigureRecurringTasksScheduleStartupTask>();
    }
}