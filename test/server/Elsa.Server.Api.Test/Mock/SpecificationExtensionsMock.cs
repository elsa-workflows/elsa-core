using Elsa.Persistence.Specifications;

namespace Elsa.Server.Api.Test.Mock
{
    public static class SpecificationExtensionsMock
    {
        public static IQueryable<T> Apply<T>(this IQueryable<T> queryable, ISpecification<T> specification) => queryable.Where(specification.ToExpression());

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
