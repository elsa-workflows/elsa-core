using System.Collections.Generic;
using System.Linq;
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
            CreateMap<WorkflowInstance, WorkflowInstanceEntity>().ForMember(d => d.Activities, d => d.ConvertUsing(new ActivityInstanceDictionaryConverter()));
            CreateMap<WorkflowInstanceEntity, WorkflowInstance>().ForMember(d => d.Activities, d => d.ConvertUsing(new ActivityInstanceEntityCollectionConverter()));
            CreateMap<ActivityDefinition, ActivityDefinitionEntity>().ReverseMap();
            CreateMap<ConnectionDefinition, ConnectionDefinitionEntity>().ReverseMap();
            CreateMap<ActivityInstance, ActivityInstanceEntity>().ReverseMap();
            CreateMap<BlockingActivity, BlockingActivityEntity>().ReverseMap();
        }
    }

    public class ActivityInstanceEntityCollectionConverter : IValueConverter<ICollection<ActivityInstanceEntity>, IDictionary<string, ActivityInstance>>
    {
        public IDictionary<string, ActivityInstance> Convert(ICollection<ActivityInstanceEntity> sourceMember, ResolutionContext context)
        {
            return sourceMember.ToDictionary(x => x.Id, x => context.Mapper.Map<ActivityInstance>(x));
        }
    }

    public class ActivityInstanceDictionaryConverter : IValueConverter<IDictionary<string, ActivityInstance>, ICollection<ActivityInstanceEntity>>
    {
        public ICollection<ActivityInstanceEntity> Convert(IDictionary<string, ActivityInstance> sourceMember, ResolutionContext context) => 
            sourceMember.Select(x => context.Mapper.Map<ActivityInstanceEntity>(x.Value)).ToList();
    }
}