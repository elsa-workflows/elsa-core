using Elsa.Models;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class ActivityDefinitionType : ObjectGraphType<ActivityDefinition>
    {
        public ActivityDefinitionType()
        {
            Name = "ActivityDefinition";

            Field(x => x.Type).Description("The activity type name.");
            Field(x => x.Description).Description("A description for the activity");
            Field(x => x.Name).Description("A name for the activity. Named activities make it easy to be referenced from workflow expressions.");
            Field(x => x.State).Description("The state of the activity");
            Field(x => x.DisplayName, type: typeof(string)).Description("A display name for this activity to display in workflow designers.");
            Field(x => x.Left).Description("The X coordinate of the activity when displayed in workflow designers.");
            Field(x => x.Top).Description("The Y coordinate of the activity when displayed in workflow designers.");
        }
    }
}