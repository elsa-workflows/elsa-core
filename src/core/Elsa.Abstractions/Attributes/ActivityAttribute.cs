using System;
using Elsa.Metadata;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ActivityAttribute : Attribute
    {
        public string? Type { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public ActivityTraits Traits { get; set; } = ActivityTraits.Action;
        public object? Outcomes { get; set; }
    }
}