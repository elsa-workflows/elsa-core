using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Persistence.Specifications
{
    public static class SpecificationCollectionsExtensions
    {
        public static ISpecification<T> ToAndSpecification<T>(this IEnumerable<ISpecification<T>> specifications) => specifications.Aggregate(Specification<T>.Identity, (current, specification) => current.And(specification));
        public static ISpecification<TChecked> ToAndSpecification<TSource, TChecked>(this IEnumerable<TSource> source, Func<TSource, ISpecification<TChecked>> specificationFactory) => source.Select(specificationFactory).ToAndSpecification();
        public static ISpecification<T> ToOrSpecification<T>(this IEnumerable<ISpecification<T>> specifications) => specifications.Aggregate(Specification<T>.None, (current, specification) => current.Or(specification));
        public static ISpecification<TChecked> ToOrSpecification<TSource, TChecked>(this IEnumerable<TSource> source, Func<TSource, ISpecification<TChecked>> specificationFactory) => source.Select(specificationFactory).ToOrSpecification();
    }
}