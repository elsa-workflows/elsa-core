using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Serialization.Formatters;
using Flowsharp.Serialization.Tokenizers;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Serialization
{
    public class WorkflowSerializer : IWorkflowSerializer
    {
        private readonly IWorkflowTokenizer workflowTokenizer;
        private readonly ITokenFormatter tokenFormatter;

        public WorkflowSerializer(IWorkflowTokenizer workflowTokenizer, ITokenFormatter tokenFormatter)
        {
            this.workflowTokenizer = workflowTokenizer;
            this.tokenFormatter = tokenFormatter;
        }
        
        public async Task<string> SerializeAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            var token = await workflowTokenizer.TokenizeAsync(workflow, cancellationToken);
            return await SerializeAsync(token, cancellationToken);
        }
        
        public Task<string> SerializeAsync(JToken token, CancellationToken cancellationToken)
        {
            var text = tokenFormatter.ToString(token);
            return Task.FromResult(text);
        }

        public Task<Workflow> DeserializeAsync(string data, CancellationToken cancellationToken)
        {
            var token = tokenFormatter.FromString(data);
            return DeserializeAsync(token, cancellationToken);
        }
        
        public Task<Workflow> DeserializeAsync(JToken token, CancellationToken cancellationToken)
        {
            return workflowTokenizer.DetokenizeAsync(token, cancellationToken);
        }
    }
}