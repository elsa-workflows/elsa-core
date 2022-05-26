// using System.Data.Entity;
// using System.Linq.Expressions;
// using Elsa.Persistence.Models;
// using Elsa.Persistence.Common.Models;
//
// namespace Elsa.Persistence.Extensions;
//
// public static class QueryableExtensions
// {
//     public static async Task<Page<TTarget>> PaginateAsync<T, TTarget>(this IQueryable<T> queryable, Expression<Func<T, TTarget>> projection, PageArgs? pageArgs = default)
//     {
//         if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
//         if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
//         
//         var count = await queryable.CountAsync();
//         var results = await queryable.Select(projection).ToListAsync();
//         return Page.Of(results, count);
//     }
//     
//     public static async Task<Page<T>> PaginateAsync<T>(this IQueryable<T> queryable, PageArgs? pageArgs = default)
//     {
//         if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
//         if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
//         
//         var count = await queryable.CountAsync();
//         var results = await queryable.ToListAsync();
//         return Page.Of(results, count);
//     }
// }