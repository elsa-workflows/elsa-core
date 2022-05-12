using AutoMapper;
using Elsa.Models;
using Elsa.Server.Api.Endpoints.WorkflowDefinitions;
using Elsa.Server.Api.Endpoints.WorkflowInstances;
using Elsa.Server.Api.Endpoints.WorkflowRegistry;
using Elsa.Services.Models;

namespace Elsa.Server.Api.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<IWorkflowBlueprint, WorkflowBlueprintModel>();
            CreateMap<IWorkflowBlueprint, WorkflowBlueprintSummaryModel>();
            CreateMap<IActivityBlueprint, ActivityBlueprintModel?>().ConvertUsing<ActivityBlueprintConverter>();
            CreateMap<ICompositeActivityBlueprint, CompositeActivityBlueprintModel>();
            CreateMap<IConnection, ConnectionModel?>().ConvertUsing<ConnectionConverter>();
            CreateMap<WorkflowInstance, WorkflowInstanceSummaryModel>();
            CreateMap<WorkflowDefinition, WorkflowDefinitionSummaryModel>();
            CreateMap<WorkflowDefinition, WorkflowDefinitionVersionModel>();
        }
    }
}