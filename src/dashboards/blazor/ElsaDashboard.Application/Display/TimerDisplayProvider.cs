using System.Collections.Generic;
using ElsaDashboard.Application.Activities;
using ElsaDashboard.Extensions;
using ElsaDashboard.Models;
using ElsaDashboard.Services;

namespace ElsaDashboard.Application.Display
{
    public class TimerDisplayProvider : ActivityDisplayProvider
    {
        protected override IEnumerable<ActivityDisplayDescriptor> GetDescriptors()
        {
            yield return new ActivityDisplayDescriptor
            {
                ActivityType = "Timer",
                RenderBody = context => builder => builder.AddActivityDisplayComponent<TimerActivity>(context)
            };
        }
    }
}