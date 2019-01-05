using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Elsa.Extensions
{
    public static class AsyncEnumerableExtensions
    {
        public static async Task<IList<T>> ToListAsync<T>(this Task<IEnumerable<T>> task)
        {
            return (await task).ToList();
        }

        public static async Task<IDictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
            this Task<IEnumerable<TSource>> task, 
            Func<TSource, TKey> keySelector)
        {
            return (await task).ToDictionary(keySelector);
        }

        public static async Task<IDictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
            this Task<IEnumerable<TSource>> task,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> valueSelector)
        {
            return (await task).ToDictionary(keySelector, valueSelector);
        }
        
        public static async Task<T> SingleAsync<T>(this Task<IEnumerable<T>> task)
        {
            return (await task).Single();
        }
        
        public static async Task<T> SingleOrDefaultAsync<T>(this Task<IEnumerable<T>> task)
        {
            return (await task).SingleOrDefault();
        }
    }
}