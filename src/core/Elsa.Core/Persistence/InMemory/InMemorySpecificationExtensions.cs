using System.Linq;
using Elsa.Persistence.Specifications;

namespace Elsa.Persistence.InMemory
{
    public static class InMemorySpecificationExtensions
    {
        public static IQueryable<T> Apply<T>(this IQueryable<T> queryable, ISpecification<T> specification) => queryable.Where(x => specification.IsSatisfiedBy(x));

        public static IQueryable<T> Apply<T>(this IQueryable<T> queryable, IOrderBy<T>? specification)
        {
            if (specification == null)
                return queryable;

            var orderByExpression = specification.OrderByExpression;
            return specification.SortDirection == SortDirection.Ascending ? queryable.OrderBy(orderByExpression) : queryable.OrderByDescending(orderByExpression);
        }

        public static IQueryable<T> Apply<T>(this IQueryable<T> queryable, IPaging? specification) => specification == null ? queryable : queryable.Skip(specification.Skip).Take(specification.Take);
    }
}