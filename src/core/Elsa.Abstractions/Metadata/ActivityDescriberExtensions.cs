using Elsa.Services;

namespace Elsa.Metadata
{
    public static class ActivityDescriberExtensions
    {
        public static ActivityDescriptor? Describe<T>(this IDescribesActivityType type) where T : IActivity => type.Describe(typeof(T));
    }
}