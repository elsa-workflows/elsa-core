using Elsa.Server.GraphQL.Models;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class WorkflowDefinitionVersionType : ObjectGraphType<WorkflowDefinitionVersionModel>
    {
        public WorkflowDefinitionVersionType()
        {
            Name = "WorkflowDefinitionVersion";
            
            Field(x => x.Id).Description("The version ID of the workflow.");
            Field(x => x.DefinitionId).Description("The ID of the workflow");
            Field(x => x.Version).Description("The version of the workflow.");
            Field(x => x.Name, true).Description("The name of the workflow.");
            Field(x => x.Description, true).Description("The description of the workflow.");
            Field(x => x.IsDisabled).Description("Whether the workflow is enabled or not.");
            Field(x => x.IsLatest).Description("Whether this is the latest version of the workflow.");
            Field(x => x.IsPublished).Description("Whether the workflow is published or not.");
            Field(x => x.IsSingleton).Description("Whether the workflow acts as a singleton or not. When true, only one instance can execute at any given time.");
            Field(x => x.Activities, true, typeof(ListGraphType<ActivityDefinitionType>)).Description("A list of activities for the workflow.");
            Field(x => x.Connections, true, typeof(ListGraphType<ConnectionDefinitionType>)).Description("A list of connections between activities on the workflow.");
        }
    }
}