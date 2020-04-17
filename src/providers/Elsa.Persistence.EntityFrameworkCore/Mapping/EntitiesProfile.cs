using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.Entities;

namespace Elsa.Persistence.EntityFrameworkCore.Mapping
{
    public class EntitiesProfile : Profile
    {
        public EntitiesProfile()
        {
            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersionEntity>()
                .ForMember(d => d.VersionId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.Id, d => d.Ignore());

            CreateMap<WorkflowDefinitionVersionEntity, WorkflowDefinitionVersion>()
                .ForCtorParam("id", p => p.MapFrom(s => s.VersionId))
                .ForMember(d => d.Id, d => d.MapFrom(s => s.VersionId));

            CreateMap<WorkflowInstance, WorkflowInstanceEntity>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ForMember(d => d.InstanceId, d => d.MapFrom(s => s.Id));

            CreateMap<WorkflowInstanceEntity, WorkflowInstance>()
                .ForMember(d => d.Id, d => d.MapFrom(s => s.InstanceId));

            CreateMap<ActivityDefinition, ActivityDefinitionEntity>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ForMember(d => d.ActivityId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.WorkflowDefinitionVersion, d => d.Ignore());

            CreateMap<ActivityDefinitionEntity, ActivityDefinition>()
                .ForCtorParam("id", p => p.MapFrom(s => s.ActivityId))
                .ForMember(d => d.Id, d => d.MapFrom(s => s.ActivityId));

            CreateMap<ActivityInstance, ActivityInstanceEntity>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ForMember(d => d.ActivityId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.WorkflowInstance, d => d.Ignore());
            
            CreateMap<ActivityInstanceEntity, ActivityInstance>().ForMember(d => d.Id, d => d.MapFrom(s => s.ActivityId));
            CreateMap<BlockingActivity, BlockingActivityEntity>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ForMember(d => d.WorkflowInstance, d => d.Ignore())
                .ReverseMap();
            
            CreateMap<ConnectionDefinition, ConnectionDefinitionEntity>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ForMember(d => d.WorkflowDefinitionVersion, d => d.Ignore())
                .ReverseMap();
        }
    }
}