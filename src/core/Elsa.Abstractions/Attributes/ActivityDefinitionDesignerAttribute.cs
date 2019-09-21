using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ActivityDefinitionDesignerAttribute : Attribute
    {
        public string Description { get; set; }
        public object Outcomes { get; set; }
    }
}