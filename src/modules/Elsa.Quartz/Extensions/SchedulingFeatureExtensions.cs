using Elsa.Quartz.Features;
using Elsa.Scheduling.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class SchedulingFeatureExtensions
{
    public static SchedulingFeature UseQuartzScheduling(this SchedulingFeature scheduling)
    {
        //scheduling.Module.Use<QuartzSchedulerFeature>();
        return scheduling;
    }
}