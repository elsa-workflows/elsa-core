using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Tokenizers
{
    public interface ITokenizer
    {
        int Priority { get; }
        bool Supports(object value);
        bool Supports(JToken value);
        JToken Tokenize(WorkflowTokenizationContext context, object value);
        object Detokenize(WorkflowTokenizationContext context, JToken value);
    }
}