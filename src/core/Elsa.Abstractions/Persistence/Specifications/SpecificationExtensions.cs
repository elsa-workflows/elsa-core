namespace Elsa.Persistence.Specifications
{
    public static class SpecificationExtensions
    {
        public static ISpecification<T> And<T>(this ISpecification<T> @this, ISpecification<T> specification)
        {
            var all = Specification<T>.All;

            if (@this == all)
                return specification;

            return specification == all ? @this : new AndSpecification<T>(@this, specification);
        }

        public static ISpecification<T> Or<T>(this ISpecification<T> @this, ISpecification<T> specification)
        {
            var all = Specification<T>.All;

            if (@this == all || specification == all)
                return all;

            return new OrSpecification<T>(@this, specification);
        }

        public static ISpecification<T> Not<T>(this ISpecification<T> @this) => new NotSpecification<T>(@this);
    }
}
