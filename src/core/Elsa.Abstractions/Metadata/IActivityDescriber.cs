using System;

namespace Elsa.Metadata
{
    public interface IActivityDescriber
    {
        ActivityInfo? Describe(Type activityType);
    }
}