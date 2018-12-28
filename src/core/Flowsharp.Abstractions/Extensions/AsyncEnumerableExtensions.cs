using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flowsharp.Extensions
{
    public static class AsyncEnumerableExtensions
    {
        public static async Task<IList<T>> ToListAsync<T>(this Task<IEnumerable<T>> task)
        {
            return (await task).ToList();
        }
    }
}