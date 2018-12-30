using System;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Extensions;
using Flowsharp.Models;
using Flowsharp.Serialization.Formatters;
using Flowsharp.Serialization.Tokenizers;
using Flowsharp.Web.Abstractions.Services;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Serialization
{
    public class WorkflowSerializer : IWorkflowSerializer
    {
        private readonly IWorkflowTokenizer workflowTokenizer;
        private readonly ITokenFormatterProvider tokenFormatterProvider;
        private readonly IActivityLibrary activityLibrary;
        private readonly IIdGenerator idGenerator;

        public WorkflowSerializer(
            IWorkflowTokenizer workflowTokenizer, 
            ITokenFormatterProvider tokenFormatterProvider,
            IActivityLibrary activityLibrary, 
            IIdGenerator idGenerator)
        {
            this.workflowTokenizer = workflowTokenizer;
            this.tokenFormatterProvider = tokenFormatterProvider;
            this.activityLibrary = activityLibrary;
            this.idGenerator = idGenerator;
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
        
        public async Task<Workflow> CloneAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            var format = JsonTokenFormatter.FormatName;
            var json = await SerializeAsync(workflow, format, cancellationToken);
            return await DeserializeAsync(json, format, cancellationToken);
        }
        
        public async Task<Workflow> DeriveAsync(Workflow parent, CancellationToken cancellationToken)
        {
            var child = await CloneAsync(parent, cancellationToken);
            child.Metadata.ParentId = parent.Metadata.Id;
            child.Metadata.Id = idGenerator.Generate();
            return child;
        }
    }
}