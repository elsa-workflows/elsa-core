using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Elsa.Serialization.Tokenizers
{
    public class TokenizerInvoker : ITokenizerInvoker
    {
        private readonly IEnumerable<ITokenizer> tokenizers;

        public TokenizerInvoker(IEnumerable<ITokenizer> tokenizers)
        {
            this.tokenizers = tokenizers;
        }
        
        public JToken Tokenize(WorkflowTokenizationContext context, object value)
        {
            var tokenizer = tokenizers.FirstOrDefault(x => x.Supports(value));
            return tokenizer?.Tokenize(context, value);
        }

        public object Detokenize(WorkflowTokenizationContext context, JToken token)
        {
            var tokenizer = tokenizers.First(x => x.Supports(token));
            return tokenizer.Detokenize(context, token);
        }
    }
}