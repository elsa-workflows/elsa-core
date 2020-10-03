using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Formatters
{
    public interface ITokenFormatter
    {
        string Format { get; }
        string ContentType { get; }
        string ToString(JObject token);
        JObject FromString(string data);
    }
}