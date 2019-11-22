using Elsa.Server.GraphQL.Models;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class WorkflowDefinitionInputType : InputObjectGraphType<WorkflowDefinitionInputModel>
    {
        public WorkflowDefinitionInputType()
        {
            Name = "WorkflowDefinitionInput";

            Field(x => x.Id).Description("The ID you wish to assign the workflow. Id no value is specified, an ID will be generated.");
            Field(x => x.Name).Description("The name of the workflow");
            Field(x => x.Description).Description("The description of the workflow.");
            Field(x => x.IsDisabled).Description("Whether the workflow should be disabled or not.");
            Field(x => x.IsSingleton).Description("Whether the workflow should act as a singleton or not. When true, only one instance can execute at any given time.");
            Field(x => x.Publish).Description("Whether the workflow should be published. If set to false, the workflow will be saved as a draft.");
            Field(x => x.Activities, type: typeof(ListGraphType<ActivityDefinitionType>)).Description("A list of activities for the workflow.");
            Field(x => x.Connections, type: typeof(ListGraphType<ConnectionDefinitionType>)).Description("A list of connections between activities on the workflow.");
        }
    }
}