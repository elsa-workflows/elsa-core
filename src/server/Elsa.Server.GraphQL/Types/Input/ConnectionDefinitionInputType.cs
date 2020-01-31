using Elsa.Models;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types.Input
{
    public class ConnectionDefinitionInputType : InputObjectGraphType<ConnectionDefinition>
    {
        public ConnectionDefinitionInputType()
        {
            Name = "ConnectionDefinitionInput";

            Field(x => x.SourceActivityId).Description("The source activity ID to connect from.");
            Field(x => x.Outcome).Description("The source activity outcome to connect from.");
            Field(x => x.DestinationActivityId).Description("The target activity ID to connect to.");
        }
    }
}