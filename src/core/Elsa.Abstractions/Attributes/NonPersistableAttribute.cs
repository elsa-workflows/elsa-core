using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class NonPersistableAttribute : Attribute
    {
    }
}