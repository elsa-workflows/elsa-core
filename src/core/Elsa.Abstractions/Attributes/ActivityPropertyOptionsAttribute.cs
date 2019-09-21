using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ActivityPropertyOptionsAttribute : Attribute
    {
        public abstract object GetOptions();
    }
}