using System.Collections.Generic;
using ElsaDashboard.Application.Activities.Timers;
using ElsaDashboard.Models;
using ElsaDashboard.Services;

namespace ElsaDashboard.Application.Display
{
    public class TimersDisplayProvider : ActivityDisplayProvider
    {
        protected override IEnumerable<ActivityDisplayDescriptor> GetDescriptors()
        {
            yield return ActivityDisplayDescriptor.For<TimerActivity>("Timer");
            yield return ActivityDisplayDescriptor.For<CronActivity>("Cron");
        }
    }
}