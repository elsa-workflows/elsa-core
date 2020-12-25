using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ContextTypeNameAttribute : Attribute
    {
        public string Name { get; }

        public ContextTypeNameAttribute(string name)
        {
            Name = name;
        }
    }
}