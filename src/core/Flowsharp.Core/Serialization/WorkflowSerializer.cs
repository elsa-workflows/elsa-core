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
        private readonly IActivityLibrary activityLibrary;

        public WorkflowSerializer(
            IWorkflowTokenizer workflowTokenizer, 
            ITokenFormatterProvider tokenFormatterProvider,
            IActivityLibrary activityLibrary)
        {
            this.workflowTokenizer = workflowTokenizer;
            this.tokenFormatterProvider = tokenFormatterProvider;
            this.activityLibrary = activityLibrary;
        }
        
        public async Task<string> SerializeAsync(Workflow workflow, string format, CancellationToken cancellationToken)
        {
            var token = await workflowTokenizer.TokenizeWorkflowAsync(workflow, cancellationToken);
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
        
        public async Task<Workflow> DeserializeAsync(JToken token, CancellationToken cancellationToken)
        {
            var workflow = await workflowTokenizer.DetokenizeWorkflowAsync(token, cancellationToken);
            var descriptors = await activityLibrary.ListAsync(cancellationToken).ToDictionaryAsync(x => x.Name);

            foreach (var activity in workflow.Activities)
            {
                activity.Descriptor = descriptors[activity.Name];
            }
            
            return workflow;
        }
    }
}