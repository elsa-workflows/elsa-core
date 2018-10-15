using Newtonsoft.Json.Linq;

namespace Flowsharp.Services
{
    public interface ITokenFormatter
    {
        string ToString(JToken token);
        JToken FromString(string data);
    }
}