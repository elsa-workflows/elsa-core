using System.Collections.Generic;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Server.GraphQL.Services;
using Elsa.Server.GraphQL.Types;
using Elsa.Server.GraphQL.Types.Input;
using Elsa.Services;
using GraphQL.Types;
using Newtonsoft.Json;

namespace Elsa.Server.GraphQL.Mutations
{
    public class UpdateWorkflowDefinition : IMutationProvider
    {
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IWorkflowPublisher publisher;

        public UpdateWorkflowDefinition(IWorkflowDefinitionStore workflowDefinitionStore, IWorkflowPublisher publisher)
        {
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.publisher = publisher;
        }
        
        public void Setup(ElsaMutation mutation)
        {
            mutation.FieldAsync<WorkflowDefinitionVersionType>(
                "updateWorkflow",
                "Update an existing workflow definition",
                new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" },
                    new QueryArgument<NonNullGraphType<UpdateWorkflowDefinitionInputType>> { Name = "workflowDefinition" }),
                async context =>
                {
                    var id = context.GetArgument<string>("id");
                    var workflowDefinitionVersion = await workflowDefinitionStore.GetByIdAsync(id, VersionOptions.Latest, context.CancellationToken);

                    if (workflowDefinitionVersion == null)
                        return null;
                    
                    var updateModel = context.GetArgument<dynamic>("workflowDefinition");
                    var props = (IDictionary<string, object>)updateModel;
                    var publish = props.ContainsKey("publish") && (bool)props["publish"];

                    // "Patch" the existing workflow definition version with the posted model values.
                    var json = JsonConvert.SerializeObject(updateModel);
                    JsonConvert.PopulateObject(json, workflowDefinitionVersion, new JsonSerializerSettings
                    {
                        ObjectCreationHandling = ObjectCreationHandling.Replace
                    });

                    if (publish)
                        workflowDefinitionVersion = await publisher.PublishAsync(workflowDefinitionVersion, context.CancellationToken);
                    else
                        workflowDefinitionVersion = await publisher.SaveDraftAsync(workflowDefinitionVersion, context.CancellationToken);

                    return workflowDefinitionVersion;
                });
        }
    }
}