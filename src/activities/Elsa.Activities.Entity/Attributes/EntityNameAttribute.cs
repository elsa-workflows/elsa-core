using System;

namespace Elsa.Activities.Entity.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EntityNameAttribute : Attribute
    {
        public string Name { get; }

        public EntityNameAttribute(string name)
        {
            Name = name;
        }
    }
}