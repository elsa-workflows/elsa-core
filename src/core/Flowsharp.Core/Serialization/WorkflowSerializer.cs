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
        
        public string Serialize(Workflow workflow)
        {
            var token = workflowTokenizer.Tokenize(workflow);
            return Serialize(token);
        }
        
        public string Serialize(JToken token)
        {
            return tokenFormatter.ToString(token);
        }

        public Workflow Deserialize(string data)
        {
            var token = tokenFormatter.FromString(data);
            return Deserialize(token);
        }
        
        public Workflow Deserialize(JToken token)
        {
            return workflowTokenizer.Detokenize(token);
        }
    }
}