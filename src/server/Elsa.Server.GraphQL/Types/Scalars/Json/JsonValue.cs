using GraphQL.Language.AST;
using Newtonsoft.Json.Linq;

namespace Elsa.Server.GraphQL.Types.Scalars.Json
{
    public class JsonValue : ValueNode<JObject>
    {
        public JsonValue(JObject value)
        {
            Value = value;
        }

        protected override bool Equals(ValueNode<JObject> node) => Value.Equals(node.Value);
    }
}