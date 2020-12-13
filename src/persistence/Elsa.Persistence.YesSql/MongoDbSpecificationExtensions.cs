using Elsa.Specifications;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql
{
    public static class YesSqlSpecificationExtensions
    {
        public static IQuery<T, TIndex> Apply<T, TIndex>(this IQuery<T, TIndex> queryable, ISpecification<T> specification) where T : class where TIndex : IIndex => queryable.Where(specification.ToExpression().ConvertType<T, TIndex>());

        public static IQuery<T, TIndex> Apply<T, TIndex>(this IQuery<T, TIndex> queryable, IGroupingSpecification<T>? specification) where TIndex : IIndex where T : class
        {
            if (specification == null)
                return queryable;

            var orderByExpression = specification.OrderByExpression.ConvertType<T, TIndex>();
            return specification.SortDirection == SortDirection.Ascending ? queryable.OrderBy(orderByExpression) : queryable.OrderByDescending(orderByExpression);
        }

        public static IQuery<T, TIndex> Apply<T, TIndex>(this IQuery<T, TIndex> queryable, IPagingSpecification? specification) where TIndex : IIndex where T : class =>
            specification == null ? queryable : (IQuery<T, TIndex>) queryable.Skip(specification.Skip).Take(specification.Take);
    }
}