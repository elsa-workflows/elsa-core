using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Elsa.ComponentTests.Helpers
{
    public static class HttpContentExtensions
    {
        public static async Task<T> ReadJsonAsync<T>(this HttpContent content)
        {
            var json = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json, JsonContent.SerializerSettings)!;
        }
    }
}