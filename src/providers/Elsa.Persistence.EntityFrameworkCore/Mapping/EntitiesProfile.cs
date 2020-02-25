using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.Entities;

namespace Elsa.Persistence.EntityFrameworkCore.Mapping
{
    public class EntitiesProfile : Profile
    {
        public EntitiesProfile()
        {
            CreateMap<ScheduledActivity, ScheduledActivityEntity>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ForMember(d => d.WorkflowInstance, d => d.Ignore())
                .ForMember(d => d.ActivityId, d => d.MapFrom(s => s.ActivityId))
                .ForMember(d => d.Input, d => d.MapFrom(s => s.Input));

            CreateMap<ScheduledActivityEntity, ScheduledActivity>()
                .ForMember(d => d.ActivityId, d => d.MapFrom(s => s.ActivityId))
                .ForMember(d => d.Input, d => d.MapFrom(s => s.Input));

            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersionEntity>()
                .ForMember(d => d.VersionId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.Id, d => d.Ignore());

            CreateMap<WorkflowDefinitionVersionEntity, WorkflowDefinitionVersion>()
                .ForCtorParam("id", p => p.MapFrom(s => s.VersionId))
                .ForMember(d => d.Id, d => d.MapFrom(s => s.VersionId));

            CreateMap<WorkflowInstance, WorkflowInstanceEntity>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ForMember(d => d.InstanceId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.ScheduledActivities, d => d.Ignore());

            CreateMap<WorkflowInstanceEntity, WorkflowInstance>()
                .ForMember(d => d.Id, d => d.MapFrom(s => s.InstanceId))
                .ForMember(d => d.ScheduledActivities, d => d.MapFrom(s => s.ScheduledActivities));

            CreateMap<ActivityDefinition, ActivityDefinitionEntity>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ForMember(d => d.ActivityId, d => d.MapFrom(s => s.Id));

            CreateMap<ActivityDefinitionEntity, ActivityDefinition>()
                .ForCtorParam("id", p => p.MapFrom(s => s.ActivityId))
                .ForMember(d => d.Id, d => d.MapFrom(s => s.ActivityId));

            CreateMap<ActivityInstance, ActivityInstanceEntity>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ForMember(d => d.ActivityId, d => d.MapFrom(s => s.Id));

            CreateMap<ActivityInstanceEntity, ActivityInstance>().ForMember(d => d.Id, d => d.MapFrom(s => s.ActivityId));
            CreateMap<BlockingActivity, BlockingActivityEntity>().ReverseMap();
            CreateMap<ConnectionDefinition, ConnectionDefinitionEntity>().ReverseMap();
        }
    }
}