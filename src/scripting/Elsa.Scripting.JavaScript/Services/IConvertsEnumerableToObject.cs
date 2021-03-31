using System.Collections;

namespace Elsa.Scripting.JavaScript.Services
{
    public interface IConvertsEnumerableToObject
    {
        object? ConvertEnumerable(IEnumerable enumerable);
    }
}