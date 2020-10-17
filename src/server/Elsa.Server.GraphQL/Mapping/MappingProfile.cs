using AutoMapper;
using Elsa.Models;
using Elsa.Server.GraphQL.Models;

namespace Elsa.Server.GraphQL.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<VersionOptionsInput, VersionOptions>().ConvertUsing<VersionOptionsConverter>();
        }
    }
}