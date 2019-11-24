using AutoMapper;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Server.GraphQL.Models;
using Elsa.Server.GraphQL.Services;
using Elsa.Server.GraphQL.Types;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Queries
{
    public class GetWorkflowDefinition : IQueryProvider
    {
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IMapper mapper;

        public GetWorkflowDefinition(IWorkflowDefinitionStore workflowDefinitionStore, IMapper mapper)
        {
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.mapper = mapper;
        }
        
        public void Setup(ElsaQuery query)
        {
            query.FieldAsync<WorkflowDefinitionVersionType>(
                "workflowDefinition",
                arguments: new QueryArguments(
                    new  QueryArgument<IdGraphType> { Name = "versionId", Description = "Get a specific workflow definition by its version ID."},
                    new  QueryArgument<StringGraphType> { Name = "definitionId", Description = "Get a specific workflow definition by ID."},
                    new QueryArgument<VersionOptionsInputType>{ Name = "version", Description = "Filter workflow definitions by the specified version options."}),
                resolve: async context =>
                {
                    var versionId = context.GetArgument<string>("versionId");

                    if (versionId != null)
                        return await workflowDefinitionStore.GetByIdAsync(versionId, context.CancellationToken);

                    var versionOptionsModel = context.GetArgument<VersionOptionsModel>("version") ?? new VersionOptionsModel { Latest = true };
                    var versionOptions = mapper.Map<VersionOptions>(versionOptionsModel);
                    var definitionId = context.GetArgument<string>("definitionId");
            
                    return await workflowDefinitionStore.GetByIdAsync(definitionId, versionOptions, context.CancellationToken);
                });
        }
    }
}