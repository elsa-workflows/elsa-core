using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ActivityDefinitionAttribute : Attribute
    {
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string RuntimeDescription { get; set; }
        public string Category { get; set; }
        public string Icon { get; set; }
        public object Outcomes { get; set; }
    }
}