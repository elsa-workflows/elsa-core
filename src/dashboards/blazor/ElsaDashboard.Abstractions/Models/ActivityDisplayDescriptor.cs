using System;
using ElsaDashboard.Components;
using ElsaDashboard.Extensions;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Models
{
    public class ActivityDisplayDescriptor
    {
        public string ActivityType { get; init; } = default!;
        public Func<ActivityDisplayContext, RenderFragment>? RenderBody { get; init; }
        public Func<ActivityDisplayContext, RenderFragment>? RenderIcon { get; init; }

        public static ActivityDisplayDescriptor For<T>(string activityType) where T : ActivityDisplayComponent =>
            new()
            {
                ActivityType = activityType,
                RenderBody = context => builder => builder.AddActivityDisplayComponent<T>(context)
            };
    }
}