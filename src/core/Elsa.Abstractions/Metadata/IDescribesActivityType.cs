using System;

namespace Elsa.Metadata
{
    public interface IDescribesActivityType
    {
        ActivityDescriptor? Describe(Type activityType);
    }
}