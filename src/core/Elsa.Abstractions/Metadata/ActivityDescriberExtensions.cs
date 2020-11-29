using Elsa.Services;

namespace Elsa.Metadata
{
    public static class ActivityDescriberExtensions
    {
        public static ActivityInfo? Describe<T>(this IActivityDescriber describer) where T : IActivity => describer.Describe(typeof(T));
    }
}