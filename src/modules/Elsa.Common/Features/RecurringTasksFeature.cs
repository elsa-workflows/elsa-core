using Elsa.Common.RecurringTasks;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Features;

public class RecurringTasksFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        Services.AddSingleton<RecurringTaskScheduleManager>();
        Services.AddStartupTask<ConfigureRecurringTasksScheduleStartupTask>();
    }
}