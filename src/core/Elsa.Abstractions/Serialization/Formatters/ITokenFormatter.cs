using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Formatters
{
    public interface ITokenFormatter
    {
        string Format { get; }
        string ContentType { get; }
        string ToString(JToken token);
        JToken FromString(string data);
    }
}