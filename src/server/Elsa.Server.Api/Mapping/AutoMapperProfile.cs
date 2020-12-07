using AutoMapper;
using Elsa.Server.Api.Endpoints.WorkflowRegistry;
using Elsa.Services.Models;

namespace Elsa.Server.Api.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<IWorkflowBlueprint, WorkflowBlueprintModel>();
            CreateMap<IActivityBlueprint, ActivityBlueprintModel>();
            CreateMap<ICompositeActivityBlueprint, CompositeActivityBlueprintModel>();
            CreateMap<IConnection, ConnectionModel>().ConvertUsing<ConnectionConverter>();
        }
    }
}