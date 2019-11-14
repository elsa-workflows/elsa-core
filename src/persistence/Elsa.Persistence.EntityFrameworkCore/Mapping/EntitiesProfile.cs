using System.Collections.Generic;
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
                .ForMember(
                    d => d.Activities, 
                    d => d..ConvertUsing<>(wdv => wdv.Activities))).ReverseMap();
            CreateMap<WorkflowInstance, WorkflowInstanceEntity>().ReverseMap();
            CreateMap<ActivityDefinition, ActivityDefinitionEntity>().ReverseMap();
            CreateMap<ConnectionDefinition, ConnectionDefinitionEntity>().ReverseMap();
            CreateMap<ActivityInstance, ActivityInstanceEntity>().ReverseMap();
            CreateMap<BlockingActivity, BlockingActivityEntity>().ReverseMap();
        }
    }

    public class MyConverter : IValueConverter<ActivityDefinition, ICollection<ActivityDefinitionEntity>>
    {
        public ICollection<ActivityDefinitionEntity> Convert(ActivityDefinition sourceMember, ResolutionContext context)
        {
            context.
        }
    }
}