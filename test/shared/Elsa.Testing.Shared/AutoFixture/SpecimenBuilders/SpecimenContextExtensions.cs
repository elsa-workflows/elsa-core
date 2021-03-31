using AutoFixture.Kernel;

namespace Elsa.Testing.Shared.AutoFixture.SpecimenBuilders
{
    public static class SpecimenContextExtensions
    {
        public static T Resolve<T>(this ISpecimenContext context) => (T) context.Resolve(typeof(T));
    }
}