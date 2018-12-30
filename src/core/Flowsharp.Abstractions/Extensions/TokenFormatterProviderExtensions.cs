using Flowsharp.Serialization;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Extensions
{
    public static class TokenFormatterProviderExtensions
    {
        public static string ToString(this ITokenFormatterProvider formatterProvider, JToken token, string format)
        {
            var formatter = formatterProvider.GetFormatter(format);
            return formatter.ToString(token);
        }
        
        public static JToken FromString(this ITokenFormatterProvider formatterProvider, string data, string format)
        {
            var formatter = formatterProvider.GetFormatter(format);
            return formatter.FromString(data);
        }
    }
}