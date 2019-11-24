using Elsa.Models;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class VariablesType : ObjectGraphType<Variables>
    {
        public VariablesType()
        {
            Name = "Variables";
            
            Field(x => x.Count).Description("The number of variables.");
        }
    }
}