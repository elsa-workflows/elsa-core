using AutoMapper;
using Elsa.Models;
using Elsa.Server.GraphQL.Types;

namespace Elsa.Server.GraphQL.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ActivityDefinitionInput, ActivityDefinition>();
        }
    }
}