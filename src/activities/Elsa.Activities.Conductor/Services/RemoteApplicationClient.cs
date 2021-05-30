using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Conductor.Options;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Elsa.Activities.Conductor.Services
{
    public class RemoteApplicationClient
    {
        private readonly HttpClient _httpClient;
        private readonly ConductorOptions _options;

        public RemoteApplicationClient(HttpClient httpClient, IOptions<ConductorOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task SendCommandAsync(string command, object? payload, CancellationToken cancellationToken = default)
        {
            payload ??= new object();
            
            var model = new
            {
                Command = command,
                Payload = payload
            };
            
            var json = JsonConvert.SerializeObject(model, _options.SerializerSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _httpClient.PostAsync("", content, cancellationToken);
        }
    }
}