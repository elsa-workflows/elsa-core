using System;

namespace Elsa.Metadata
{
    public interface IDescribeActivityType
    {
        ActivityDescriptor? Describe(Type activityType);
    }
}