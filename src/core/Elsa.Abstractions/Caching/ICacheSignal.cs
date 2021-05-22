using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Elsa.Caching
{
    /// <summary>
    /// Provides change tokens for memory caches, allowing code to evict cache entries by triggering a signal.
    /// </summary>
    public interface ICacheSignal
    {
        IChangeToken GetToken(string key);
        void TriggerToken(string key);
        ValueTask TriggerTokenAsync(string key);
    }
}