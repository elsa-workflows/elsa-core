using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.Entities;
using Elsa.Services;

namespace Elsa.Persistence.EntityFrameworkCore.Mapping
{
    public class EntitiesProfile : MappingProfile
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
                .ForMember(d => d.Activities, d => d.ConvertUsing(new ActivityInstanceDictionaryConverter()))
                .ForMember(d => d.InstanceId, d => d.MapFrom(s => s.Id));

            CreateMap<WorkflowInstanceEntity, WorkflowInstance>()
                .ForMember(d => d.Activities, d => d.ConvertUsing(new ActivityInstanceEntityCollectionConverter()))
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

    public class ActivityInstanceEntityCollectionConverter : IValueConverter<ICollection<ActivityInstanceEntity>, IDictionary<string, ActivityInstance>>
    {
        public IDictionary<string, ActivityInstance> Convert(ICollection<ActivityInstanceEntity> sourceMember, ResolutionContext context)
        {
            return sourceMember.ToDictionary(x => x.ActivityId, x => context.Mapper.Map<ActivityInstance>(x));
        }
    }

    public class ActivityInstanceDictionaryConverter : IValueConverter<IDictionary<string, ActivityInstance>, ICollection<ActivityInstanceEntity>>
    {
        public ICollection<ActivityInstanceEntity> Convert(IDictionary<string, ActivityInstance> sourceMember, ResolutionContext context) =>
            sourceMember.Select(x => context.Mapper.Map<ActivityInstanceEntity>(x.Value)).ToList();
    }
}