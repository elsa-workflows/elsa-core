using Newtonsoft.Json.Linq;

namespace Flowsharp.Serialization.Formatters
{
    public interface ITokenFormatter
    {
        string ToString(JToken token);
        JToken FromString(string data);
    }
}