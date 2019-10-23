using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ActivityPropertyAttribute : Attribute
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }
        public string Hint { get; set; }
    }
}