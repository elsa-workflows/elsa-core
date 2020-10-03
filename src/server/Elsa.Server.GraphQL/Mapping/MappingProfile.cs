using AutoMapper;
using Elsa.Models;
using Elsa.Server.GraphQL.Models;
using Elsa.Server.GraphQL.Types;

namespace Elsa.Server.GraphQL.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ActivityDefinitionInput, ActivityDefinitionRecord>()
                .ForMember(d => d.State, d => d.MapFrom<ActivityStateResolver>());
            
            CreateMap<VersionOptionsInput, VersionOptions>().ConvertUsing<VersionOptionsConverter>();
        }
    }
}