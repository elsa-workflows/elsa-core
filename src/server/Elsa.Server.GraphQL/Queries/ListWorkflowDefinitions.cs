using AutoMapper;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Server.GraphQL.Models;
using Elsa.Server.GraphQL.Services;
using Elsa.Server.GraphQL.Types;
using GraphQL.Types;

namespace Elsa.Server.GraphQL.Queries
{
    public class ListWorkflowDefinitions : IQueryProvider
    {
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IMapper mapper;

        public ListWorkflowDefinitions(IWorkflowDefinitionStore workflowDefinitionStore, IMapper mapper)
        {
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.mapper = mapper;
        }
        
        public void Setup(ElsaQuery query)
        {
            query.FieldAsync<ListGraphType<WorkflowDefinitionVersionType>>(
                "workflowDefinitions",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<VersionOptionsInputType>>{ Name = "version", Description = "Filter workflow definitions by the specified version options."}),
                resolve: async context =>
                {
                    var versionOptionsModel = context.GetArgument<VersionOptionsModel>("version");
                    var versionOptions = mapper.Map<VersionOptions>(versionOptionsModel);
                    return await workflowDefinitionStore.ListAsync(versionOptions, context.CancellationToken);
                });
        }
    }
}