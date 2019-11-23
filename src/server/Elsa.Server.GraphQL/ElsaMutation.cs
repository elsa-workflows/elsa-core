using System.Collections.Generic;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Server.GraphQL.Models;
using Elsa.Server.GraphQL.Types;
using Elsa.Services;
using GraphQL.Types;
using Newtonsoft.Json;

namespace Elsa.Server.GraphQL
{
    public class ElsaMutation : ObjectGraphType
    {
        public ElsaMutation(IWorkflowDefinitionStore workflowDefinitionStore, IWorkflowPublisher publisher, IIdGenerator idGenerator, IMapper mapper)
        {
            FieldAsync<WorkflowDefinitionVersionType>(
                "defineWorkflow",
                "Create a new workflow definitions",
                new QueryArguments(new QueryArgument<NonNullGraphType<DefineWorkflowDefinitionInputType>> { Name = "workflowDefinition" }),
                async context =>
                {
                    var model = context.GetArgument<DefineWorkflowDefinitionInputModel>("workflowDefinition");
                    var publish = model.Publish;
                    var workflowDefinitionVersion = mapper.Map<WorkflowDefinitionVersion>(model);

                    workflowDefinitionVersion.Id = idGenerator.Generate();

                    if (string.IsNullOrWhiteSpace(workflowDefinitionVersion.DefinitionId))
                        workflowDefinitionVersion.DefinitionId = idGenerator.Generate();

                    if (publish)
                        await publisher.PublishAsync(workflowDefinitionVersion, context.CancellationToken);
                    else
                        await publisher.SaveDraftAsync(workflowDefinitionVersion, context.CancellationToken);

                    return mapper.Map<WorkflowDefinitionVersionModel>(workflowDefinitionVersion);
                });
            
            FieldAsync<WorkflowDefinitionVersionType>(
                "updateWorkflow",
                "Update an existing workflow definition",
                new QueryArguments(
                    new QueryArgument<NonNullGraphType<IdGraphType>> { Name = "id" },
                    new QueryArgument<NonNullGraphType<UpdateWorkflowDefinitionInputType>> { Name = "workflowDefinition" }),
                async context =>
                {
                    var id = context.GetArgument<string>("id");
                    var workflowDefinitionVersion = await workflowDefinitionStore.GetByIdAsync(id, VersionOptions.Latest, context.CancellationToken);

                    if (workflowDefinitionVersion == null)
                        return null;

                    var workflowDefinitionVersionModel = mapper.Map<WorkflowDefinitionVersionModel>(workflowDefinitionVersion);
                    var updateModel = context.GetArgument<dynamic>("workflowDefinition");
                    var props = (IDictionary<string, object>)updateModel;
                    var publish = props.ContainsKey("publish") && (bool)props["publish"];

                    // "Patch" the existing workflow definition version with the posted model values.
                    var json = JsonConvert.SerializeObject(updateModel);
                    JsonConvert.PopulateObject(json, workflowDefinitionVersionModel, new JsonSerializerSettings
                    {
                        ObjectCreationHandling = ObjectCreationHandling.Replace
                    });

                    workflowDefinitionVersion = mapper.Map<WorkflowDefinitionVersion>(workflowDefinitionVersionModel);
                    
                    if (publish)
                        workflowDefinitionVersion = await publisher.PublishAsync(workflowDefinitionVersion, context.CancellationToken);
                    else
                        workflowDefinitionVersion = await publisher.SaveDraftAsync(workflowDefinitionVersion, context.CancellationToken);

                    return mapper.Map<WorkflowDefinitionVersionModel>(workflowDefinitionVersion);
                });
        }
    }
}