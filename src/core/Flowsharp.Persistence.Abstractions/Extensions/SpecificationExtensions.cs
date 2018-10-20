using Flowsharp.Persistence.Primitives;
using Flowsharp.Persistence.Specifications;

namespace Flowsharp.Persistence.Extensions
{
    public static class SpecificationExtensions
    {
        public static ISpecification<T, TVisitor> And<T, TVisitor>(this ISpecification<T, TVisitor> left, ISpecification<T, TVisitor> right)
            where TVisitor : ISpecificationVisitor<TVisitor, T>
        {
            return new AndSpecification<T, TVisitor>(left, right);
        }

        public static ISpecification<T, TVisitor> Or<T, TVisitor>(this ISpecification<T, TVisitor> left, ISpecification<T, TVisitor> right)
            where TVisitor : ISpecificationVisitor<TVisitor, T>
        {
            return new OrSpecification<T, TVisitor>(left, right);
        }

        public static ISpecification<T, TVisitor> Not<T, TVisitor>(this ISpecification<T, TVisitor> specification)
            where TVisitor : ISpecificationVisitor<TVisitor, T>
        {
            return new NotSpecification<T, TVisitor>(specification);
        }
    }
}
