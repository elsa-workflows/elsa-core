using Elsa.Features.Extensions;
using Elsa.Features.Services;
using Elsa.Quartz.Features;
using Elsa.Scheduling.Features;

namespace Elsa.Quartz.Extensions;

public static class SchedulingFeatureExtensions
{
    public static SchedulingFeature UseQuartzScheduling(this SchedulingFeature scheduling)
    {
        scheduling.Module.Use<QuartzSchedulerFeature>();
        return scheduling;
    }
}