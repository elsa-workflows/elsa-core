using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Server.GraphQL.Models;
using Elsa.Server.GraphQL.Types;
using GraphQL.Types;

namespace Elsa.Server.GraphQL
{
    public class ElsaQuery : ObjectGraphType
    {
        private readonly IWorkflowDefinitionStore workflowDefinitionStore;
        private readonly IMapper mapper;

        public ElsaQuery(IWorkflowDefinitionStore workflowDefinitionStore, IMapper mapper)
        {
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.mapper = mapper;

            FieldAsync<ListGraphType<WorkflowDefinitionVersionType>>(
                "workflowDefinitions",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<VersionOptionsInputType>>{ Name = "version", Description = "Filter workflow definitions by the specified version options."}),
                resolve: ResolveWorkflowDefinitions);
            
            FieldAsync<WorkflowDefinitionVersionType>(
                "workflowDefinition",
                arguments: new QueryArguments(
                    new  QueryArgument<IdGraphType> { Name = "versionId", Description = "Get a specific workflow definition by its version ID."},
                    new  QueryArgument<StringGraphType> { Name = "definitionId", Description = "Get a specific workflow definition by ID."},
                new QueryArgument<VersionOptionsInputType>{ Name = "version", Description = "Filter workflow definitions by the specified version options."}),
                resolve: ResolveWorkflowDefinition);
        }

        private async Task<object> ResolveWorkflowDefinitions(ResolveFieldContext<object> context)
        {
            var versionOptionsModel = context.GetArgument<VersionOptionsModel>("version");
            var versionOptions = mapper.Map<VersionOptions>(versionOptionsModel);
            var items = await workflowDefinitionStore.ListAsync(versionOptions, context.CancellationToken);

            return mapper.Map<IEnumerable<WorkflowDefinitionVersionModel>>(items);
        }
        
        private async Task<object> ResolveWorkflowDefinition(ResolveFieldContext<object> context)
        {
            var workflowDefinition = await ResolveWorkflowDefinitionInternal(context);

            return mapper.Map<WorkflowDefinitionVersionModel>(workflowDefinition);
        }
        
        private async Task<WorkflowDefinitionVersion> ResolveWorkflowDefinitionInternal(ResolveFieldContext<object> context)
        {
            var versionId = context.GetArgument<string>("versionId");

            if (versionId != null)
                return await workflowDefinitionStore.GetByIdAsync(versionId, context.CancellationToken);

            var versionOptionsModel = context.GetArgument<VersionOptionsModel>("version") ?? new VersionOptionsModel { Latest = true };
            var versionOptions = mapper.Map<VersionOptions>(versionOptionsModel);
            var definitionId = context.GetArgument<string>("definitionId");
            
            return await workflowDefinitionStore.GetByIdAsync(definitionId, versionOptions, context.CancellationToken);
        }
    }
}