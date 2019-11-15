using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.Entities;

namespace Elsa.Persistence.EntityFrameworkCore.Mapping
{
    public class EntitiesProfile : Profile
    {
        public EntitiesProfile()
        {
            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersionEntity>().ReverseMap();
            CreateMap<WorkflowInstance, WorkflowInstanceEntity>().ReverseMap();
            CreateMap<ActivityDefinition, ActivityDefinitionEntity>().ReverseMap();
            CreateMap<ConnectionDefinition, ConnectionDefinitionEntity>().ReverseMap();
            CreateMap<ActivityInstance, ActivityInstanceEntity>().ReverseMap();
            CreateMap<BlockingActivity, BlockingActivityEntity>().ReverseMap();
        }
    }
}