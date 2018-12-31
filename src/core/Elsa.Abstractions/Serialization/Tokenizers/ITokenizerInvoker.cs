using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Tokenizers
{
    public interface ITokenizerInvoker
    {
        JToken Tokenize(WorkflowTokenizationContext context, object value);
        object Detokenize(WorkflowTokenizationContext context, JToken token);
    }
}