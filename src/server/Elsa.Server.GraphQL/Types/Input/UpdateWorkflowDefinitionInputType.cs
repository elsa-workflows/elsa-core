using Elsa.Server.GraphQL.Models;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types.Input
{
    public class UpdateWorkflowDefinitionInputType : InputObjectGraphType<UpdateWorkflowDefinitionInputModel>
    {
        public UpdateWorkflowDefinitionInputType()
        {
            Name = "UpdateWorkflowDefinitionInput";
            
            Field(x => x.Name, true).Description("The name of the workflow");
            Field(x => x.Description, true).Description("The description of the workflow.");
            Field(x => x.IsDisabled, true).Description("Whether the workflow should be disabled or not.");
            Field(x => x.IsSingleton, true).Description("Whether the workflow should act as a singleton or not. When true, only one instance can execute at any given time.");
            Field(x => x.Publish, true).Description("Whether the workflow should be published. If set to false, the workflow will be saved as a draft.");
            Field(x => x.Activities, true, typeof(ListGraphType<ActivityDefinitionInputType>)).Description("A list of activities for the workflow.");
            Field(x => x.Connections, true, typeof(ListGraphType<ConnectionDefinitionInputType>)).Description("A list of connections between activities on the workflow.");
        }
    }
}