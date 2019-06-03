using Elsa.Serialization.Tokenizers;
using Newtonsoft.Json.Linq;

namespace Elsa.Core.Serialization.Tokenizers
{
    public abstract class Tokenizer<T> : ITokenizer
    {
        public virtual int Priority => 0;
        public virtual bool Supports(JToken value) => true;
        
        protected abstract JToken Tokenize(WorkflowTokenizationContext context, T value);
        protected virtual bool Supports(T value) => true;
        protected abstract T Detokenize(WorkflowTokenizationContext context, JToken value);

        bool ITokenizer.Supports(object value) => value is T v && Supports(v);
        JToken ITokenizer.Tokenize(WorkflowTokenizationContext context, object value) => Tokenize(context, (T)value);
        object ITokenizer.Detokenize(WorkflowTokenizationContext context, JToken value) => Detokenize(context, value);
    }
}