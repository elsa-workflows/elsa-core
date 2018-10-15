using Flowsharp.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Serialization.Formatters
{
    public class JsonTokenFormatter : ITokenFormatter
    {
        public string ToString(JToken token) => token.ToString(Formatting.None);
        public JToken FromString(string data) => JToken.Parse(data);
    }
}