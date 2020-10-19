using Elsa.Models;
using Elsa.Serialization;

namespace Elsa
{
    public static class VariablesExtensions
    {
        public static T Get<T>(this Variables variables, string name, IContentSerializer serializer)
        {
            var value = variables.Get(name);
            return value != null ? serializer.Deserialize<T>(value) : default!;
        }
    }
}