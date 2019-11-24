using GraphQL.Language.AST;
using GraphQL.Types;
using Newtonsoft.Json.Linq;

namespace Elsa.Server.GraphQL.Types.Scalars.Json
{
    public class JsonType : ScalarGraphType
    {
        public JsonType()
        {
            Name = "Json";
        }
        
        public override object Serialize(object value) => value;

        public override object ParseValue(object value) => JObject.Parse((string)value);

        public override object ParseLiteral(IValue value)
        {
            if (value is JsonValue jsonValue)
                return ParseValue(jsonValue.Value);
            
            return value is StringValue stringValue 
                ? JObject.Parse(stringValue.Value) 
                : null;
        }
    }
}