using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Services;
using Newtonsoft.Json;

namespace Flowsharp.Serialization
{
    public class JsonWorkflowSerializer : IWorkflowSerializer
    {
        private readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            TypeNameHandling = TypeNameHandling.Objects
        };
        
        public Task<string> SerializeAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(workflow, settings);
            return Task.FromResult(json);
        }

        public Task<Workflow> DeserializeAsync(string json, CancellationToken none)
        {
            var workflow = JsonConvert.DeserializeObject<Workflow>(json, settings);
            return Task.FromResult(workflow);
        }
    }
}