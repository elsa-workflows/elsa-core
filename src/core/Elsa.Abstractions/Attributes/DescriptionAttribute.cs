using System;

namespace Elsa.Attributes
{
    public class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description)
        {
            Description = description;
        }
        
        public string Description { get; }
    }
}