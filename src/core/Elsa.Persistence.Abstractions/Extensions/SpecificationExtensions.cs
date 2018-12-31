using Elsa.Persistence.Specifications.Primitives;

namespace Elsa.Persistence.Extensions
{
    public static class SpecificationExtensions
    {
        public static ISpecification<T, TVisitor> And<T, TVisitor>(this ISpecification<T, TVisitor> left, ISpecification<T, TVisitor> right)
            where TVisitor : ISpecificationVisitor<TVisitor, T>
        {
            return new And<T, TVisitor>(left, right);
        }

        public static ISpecification<T, TVisitor> Or<T, TVisitor>(this ISpecification<T, TVisitor> left, ISpecification<T, TVisitor> right)
            where TVisitor : ISpecificationVisitor<TVisitor, T>
        {
            return new Or<T, TVisitor>(left, right);
        }

        public static ISpecification<T, TVisitor> Not<T, TVisitor>(this ISpecification<T, TVisitor> specification)
            where TVisitor : ISpecificationVisitor<TVisitor, T>
        {
            return new Not<T, TVisitor>(specification);
        }
    }
}
