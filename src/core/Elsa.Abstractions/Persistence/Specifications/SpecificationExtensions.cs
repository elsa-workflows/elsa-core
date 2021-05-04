namespace Elsa.Persistence.Specifications
{
    public static class SpecificationExtensions
    {
        public static ISpecification<T> And<T>(this ISpecification<T> @this, ISpecification<T> specification)
        {
            var identity = Specification<T>.Identity;

            if (@this == identity)
                return specification;

            return specification == identity ? @this : new AndSpecification<T>(@this, specification);
        }

        public static ISpecification<T> Or<T>(this ISpecification<T> @this, ISpecification<T> specification)
        {
            var identity = Specification<T>.Identity;

            if (@this == identity || specification == identity)
                return identity;

            return new OrSpecification<T>(@this, specification);
        }

        public static ISpecification<T> Not<T>(this ISpecification<T> @this) => new NotSpecification<T>(@this);
    }
}
