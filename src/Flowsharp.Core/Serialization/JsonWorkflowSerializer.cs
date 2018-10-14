using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Services;
using Newtonsoft.Json;

namespace Flowsharp.Serialization
{
    public class JsonWorkflowSerializer : IWorkflowSerializer
    {
        public Task<string> SerializeAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            var settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                TypeNameHandling = TypeNameHandling.Objects
            };
            var json = JsonConvert.SerializeObject(workflow, settings);
            return Task.FromResult(json);
        }
    }
}