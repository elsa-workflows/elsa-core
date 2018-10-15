using Flowsharp.Serialization;
using Newtonsoft.Json.Linq;

namespace Flowsharp.Services
{
    public interface ITokenizerInvoker
    {
        JToken Tokenize(WorkflowTokenizationContext context, object value);
        object Detokenize(WorkflowTokenizationContext context, JToken token);
    }
}