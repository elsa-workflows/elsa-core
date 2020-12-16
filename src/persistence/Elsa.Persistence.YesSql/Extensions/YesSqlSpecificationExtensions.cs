using Elsa.Persistence.Specifications;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Extensions
{
    public static class YesSqlSpecificationExtensions
    {
        public static IQuery<TDocument, TIndex> Apply<T, TDocument, TIndex>(this IQuery<TDocument, TIndex> queryable, ISpecification<T> specification) where T : class where TIndex : IIndex where TDocument : class =>
            queryable.Where(specification.ToExpression().ConvertType<T, TDocument>().ConvertType<TDocument, TIndex>());

        public static IQuery<TDocument, TIndex> Apply<T, TDocument, TIndex>(this IQuery<TDocument, TIndex> queryable, IOrderBy<T>? specification) where TIndex : IIndex where T : class where TDocument : class
        {
            if (specification == null)
                return queryable;

            var orderByExpression = specification.OrderByExpression.ConvertType<T, TDocument>().ConvertType<TDocument, TIndex>();
            return specification.SortDirection == SortDirection.Ascending ? queryable.OrderBy(orderByExpression) : queryable.OrderByDescending(orderByExpression);
        }

        public static IQuery<TDocument, TIndex> Apply<TDocument, TIndex>(this IQuery<TDocument, TIndex> queryable, IPaging? specification) where TIndex : IIndex where TDocument : class =>
            specification == null ? queryable : (IQuery<TDocument, TIndex>) queryable.Skip(specification.Skip).Take(specification.Take);
    }
}