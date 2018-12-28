using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Extensions;
using Flowsharp.Models;
using Flowsharp.Serialization.Tokenizers;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Serialization
{
    public class WorkflowSerializer : IWorkflowSerializer
    {
        private readonly IWorkflowTokenizer workflowTokenizer;
        private readonly ITokenFormatterProvider tokenFormatterProvider;

        public WorkflowSerializer(IWorkflowTokenizer workflowTokenizer, ITokenFormatterProvider tokenFormatterProvider)
        {
            this.workflowTokenizer = workflowTokenizer;
            this.tokenFormatterProvider = tokenFormatterProvider;
        }
        
        public async Task<string> SerializeAsync(Workflow workflow, string format, CancellationToken cancellationToken)
        {
            var token = await workflowTokenizer.TokenizeAsync(workflow, cancellationToken);
            return await SerializeAsync(token, format, cancellationToken);
        }
        
        public Task<string> SerializeAsync(JToken token, string format, CancellationToken cancellationToken)
        {
            var text = tokenFormatterProvider.ToString(token, format);
            return Task.FromResult(text);
        }

        public Task<Workflow> DeserializeAsync(string data, string format, CancellationToken cancellationToken)
        {
            var token = tokenFormatterProvider.FromString(data, format);
            return DeserializeAsync(token, cancellationToken);
        }
        
        public Task<Workflow> DeserializeAsync(JToken token, CancellationToken cancellationToken)
        {
            return workflowTokenizer.DetokenizeAsync(token, cancellationToken);
        }
    }
}