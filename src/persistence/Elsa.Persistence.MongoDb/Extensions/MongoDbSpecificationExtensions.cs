using Elsa.Persistence.Specifications;
using MongoDB.Driver.Linq;

namespace Elsa.Persistence.MongoDb
{
    public static class MongoDbSpecificationExtensions
    {
        public static IMongoQueryable<T> Apply<T>(this IMongoQueryable<T> queryable, ISpecification<T> specification) => queryable.Where(specification.ToExpression());

        public static IMongoQueryable<T> Apply<T>(this IMongoQueryable<T> queryable, IOrderBy<T>? specification)
        {
            if (specification == null)
                return queryable;

            var orderByExpression = specification.OrderByExpression;
            return specification.SortDirection == SortDirection.Ascending ? queryable.OrderBy(orderByExpression) : queryable.OrderByDescending(orderByExpression);
        }

        public static IMongoQueryable<T> Apply<T>(this IMongoQueryable<T> queryable, IPaging? specification) => specification == null ? queryable : queryable.Skip(specification.Skip).Take(specification.Take);
    }
}