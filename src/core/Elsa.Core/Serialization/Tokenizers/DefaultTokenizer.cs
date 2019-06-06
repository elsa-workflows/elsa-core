using Elsa.Serialization.Tokenizers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Core.Serialization.Tokenizers
{
    public class DefaultTokenizer : Tokenizer<object>
    {
        public override int Priority => -1;

        protected override JToken Tokenize(WorkflowTokenizationContext context, object value)
        {
            return JToken.FromObject(value, new JsonSerializer { TypeNameHandling = TypeNameHandling.All });
        }
        
        protected override object Detokenize(WorkflowTokenizationContext context, JToken value)
        {
            return value.ToObject<object>(new JsonSerializer { TypeNameHandling = TypeNameHandling.All });
        }
    }
}