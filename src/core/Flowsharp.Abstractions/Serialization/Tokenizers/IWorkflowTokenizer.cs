using Flowsharp.Models;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Serialization.Tokenizers
{
    public interface IWorkflowTokenizer
    {
        JToken Tokenize(Workflow value);
        Workflow Detokenize(JToken token);
    }
}