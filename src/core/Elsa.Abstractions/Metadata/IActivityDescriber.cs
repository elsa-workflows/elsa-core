using System;

namespace Elsa.Metadata
{
    public interface IActivityDescriber
    {
        ActivityDescriptor? Describe(Type activityType);
    }
}