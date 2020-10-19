using System.Net.Http;
using System.Threading.Tasks;

namespace Elsa.ComponentTests.Helpers
{
    public static class HttpClientExtensions
    {
        public static async Task<T> GetJsonAsync<T>(this HttpClient httpClient, string requestUri)
        {
            using var response = await httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadJsonAsync<T>();
        }

        public static async Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient httpClient, string requestUri, T value) => await httpClient.PostAsync(requestUri, new JsonContent(value!));
    }
}