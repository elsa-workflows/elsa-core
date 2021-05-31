using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Options;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Elsa.Activities.Conductor.Services
{
    public class ApplicationCommandsClient
    {
        private readonly HttpClient _httpClient;
        private readonly ConductorOptions _options;

        public ApplicationCommandsClient(HttpClient httpClient, IOptions<ConductorOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task SendCommandAsync(SendCommandModel command, CancellationToken cancellationToken = default)
        {
            var json = JsonConvert.SerializeObject(command, _options.SerializerSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("", content, cancellationToken);
            
            // TODO: Handle response. If it contains instructions to resume the workflow, we can do so immediately.
        }
    }
}