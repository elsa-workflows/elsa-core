using Microsoft.Extensions.Primitives;

namespace Elsa.Caching
{
    /// <summary>
    /// Provides change tokens for memory caches, allowing code to evict cache entries by triggering a signal.
    /// </summary>
    public interface ISignal
    {
        IChangeToken GetToken(string key);
        void Trigger(string key);
    }
}