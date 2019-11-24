using Elsa.Models;
using Elsa.Server.GraphQL.Scalars.Json;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class ActivityDefinitionType : ObjectGraphType<ActivityDefinition>
    {
        public ActivityDefinitionType()
        {
            Name = "ActivityDefinition";

            Field(x => x.Type).Description("The activity type name.");
            Field(x => x.Description, true).Description("A description for the activity");
            Field(x => x.Name, true).Description("A name for the activity. Named activities make it easy to be referenced from workflow expressions.");
            Field(x => x.State, true, typeof(JsonType)).Description("The state of the activity");
            Field(x => x.DisplayName, true).Description("A display name for this activity to display in workflow designers.");
            Field(x => x.Left, true).Description("The X coordinate of the activity when displayed in workflow designers.");
            Field(x => x.Top, true).Description("The Y coordinate of the activity when displayed in workflow designers.");
        }
    }
}