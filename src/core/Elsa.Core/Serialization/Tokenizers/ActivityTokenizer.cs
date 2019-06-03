using Elsa.Serialization.Tokenizers;
using Newtonsoft.Json.Linq;

namespace Elsa.Core.Serialization.Tokenizers
{
    public class ActivityTokenizer : Tokenizer<IActivity>
    {
        public override bool Supports(JToken value) => value is JObject obj && obj.ContainsKey("activityId");
        
        protected override JToken Tokenize(WorkflowTokenizationContext context, IActivity value)
        {
            var activityId = context.ActivityIdLookup[value];
            return new JObject(new { activityId });
        }
        
        protected override IActivity Detokenize(WorkflowTokenizationContext context, JToken value)
        {
            var obj = (JObject) value;
            var activityId = obj["activityId"].Value<string>();
            return context.ActivityLookup[activityId];
        }
    }
}