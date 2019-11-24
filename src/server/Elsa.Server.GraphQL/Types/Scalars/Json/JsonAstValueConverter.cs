using GraphQL.Language.AST;
using GraphQL.Types;
using Newtonsoft.Json.Linq;

namespace Elsa.Server.GraphQL.Types.Scalars.Json
{
    public class JsonAstValueConverter : IAstFromValueConverter
    {
        public bool Matches(object value, IGraphType type) => value is JObject;
        public IValue Convert(object value, IGraphType type) => new JsonValue((JObject)value);
    }
}