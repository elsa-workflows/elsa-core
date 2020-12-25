using System;
using Microsoft.AspNetCore.Components;

namespace ElsaDashboard.Models
{
    public class ActivityDisplayDescriptor
    {
        public string ActivityType { get; init; } = default!;
        public Func<ActivityDisplayContext, RenderFragment>? RenderBody { get; init; }
        public Func<ActivityDisplayContext, RenderFragment>? RenderIcon { get; init; }
    }
}