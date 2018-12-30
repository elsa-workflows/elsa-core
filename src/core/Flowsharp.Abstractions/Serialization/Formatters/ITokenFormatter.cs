using Newtonsoft.Json.Linq;

namespace Flowsharp.Serialization.Formatters
{
    public interface ITokenFormatter
    {
        string Format { get; }
        string ContentType { get; }
        string ToString(JToken token);
        JToken FromString(string data);
    }
}