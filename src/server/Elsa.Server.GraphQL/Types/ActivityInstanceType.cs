using Elsa.Models;
using Elsa.Server.GraphQL.Scalars.Json;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class ActivityInstanceType : ObjectGraphType<ActivityInstance>
    {
        public ActivityInstanceType()
        {
            Name = "ActivityInstance";
            Description = "Represents an instance of an activity definition on a workflow instance.";

            Field(x => x.Id).Description("The ID of the activity.");
            Field(x => x.Type).Description("The activity type.");
            Field(x => x.State, type: typeof(JsonType)).Description("Holds state specific to this activity.");
            Field(x => x.Output, type: typeof(JsonType)).Description("Holds output state provided by this activity.");
        }
    }
}