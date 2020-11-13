using System;

namespace Elsa.Client.Models
{
    public class ActivityDescriptor
    {
        public string Type { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string? Description { get; set; }
        public string? RuntimeDescription { get; set; }
        public string Category { get; set; } = default!;
        public string? Icon { get; set; }
        public string[] Outcomes { get; set; } = new string[0];
        public ActivityPropertyDescriptor[] Properties { get; set; } = new ActivityPropertyDescriptor[0];
    }
}