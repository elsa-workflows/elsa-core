using Microsoft.Extensions.Primitives;

namespace Elsa.Caching
{
    public interface ISignal
    {
        IChangeToken GetToken(string key);
        void Trigger(string key);
    }
}