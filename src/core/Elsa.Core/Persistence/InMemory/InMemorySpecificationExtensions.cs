using System.Linq;
using Elsa.Specifications;

namespace Elsa.Persistence.InMemory
{
    public static class InMemorySpecificationExtensions
    {
        public static IQueryable<T> Apply<T>(this IQueryable<T> queryable, ISpecification<T> specification) => queryable.Where(specification.ToExpression());

        public static IQueryable<T> Apply<T>(this IQueryable<T> queryable, IGroupingSpecification<T>? specification)
        {
            if (specification == null)
                return queryable;

            var orderByExpression = specification.OrderByExpression;
            return specification.SortDirection == SortDirection.Ascending ? queryable.OrderBy(orderByExpression) : queryable.OrderByDescending(orderByExpression);
        }

        public static IQueryable<T> Apply<T>(this IQueryable<T> queryable, IPagingSpecification? specification) => specification == null ? queryable : queryable.Skip(specification.Skip).Take(specification.Take);
    }
}