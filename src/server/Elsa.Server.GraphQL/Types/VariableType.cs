using Elsa.Models;
using Elsa.Server.GraphQL.Scalars.Json;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class VariableType : ObjectGraphType<Variable>
    {
        public VariableType()
        {
            Name = "Variable";

            Field(x => x.Value, true, typeof(JsonType)).Description("The value of the variable.");
        }
    }
}