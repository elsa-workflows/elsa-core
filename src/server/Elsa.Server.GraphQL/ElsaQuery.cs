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
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IMapper mapper;

        public ElsaQuery(IWorkflowDefinitionStore workflowDefinitionStore, IWorkflowInstanceStore workflowInstanceStore, IMapper mapper)
        {
            this.workflowDefinitionStore = workflowDefinitionStore;
            this.workflowInstanceStore = workflowInstanceStore;
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
            
            FieldAsync<ListGraphType<WorkflowInstanceType>>(
                "workflowInstances",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType>{ Name = "definitionId", Description = "Filter workflow instances by the specified workflow definition ID."},
                    new QueryArgument<EnumerationGraphType<WorkflowStatus>>{ Name = "status", Description = "Filter workflow instances by the specified workflow status."}
                ),
                resolve: ResolveWorkflowInstances);
        }

        private async Task<object> ResolveWorkflowInstances(ResolveFieldContext<object> context)
        {
            return await workflowInstanceStore.ListAllAsync(context.CancellationToken);
        }

        private async Task<object> ResolveWorkflowDefinitions(ResolveFieldContext<object> context)
        {
            var versionOptionsModel = context.GetArgument<VersionOptionsModel>("version");
            var versionOptions = mapper.Map<VersionOptions>(versionOptionsModel);
            return await workflowDefinitionStore.ListAsync(versionOptions, context.CancellationToken);
        }
        
        private async Task<object> ResolveWorkflowDefinition(ResolveFieldContext<object> context) => await ResolveWorkflowDefinitionInternal(context);

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