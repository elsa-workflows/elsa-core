using AutoMapper;
using Elsa.Models;
using Elsa.Server.GraphQL.Models;

namespace Elsa.Server.GraphQL.Mapping
{
    public class GraphQLProfile : Profile
    {
        public GraphQLProfile()
        {
            CreateMap<DefineWorkflowDefinitionInputModel, WorkflowDefinitionVersion>();

            CreateMap<VersionOptionsModel, VersionOptions>()
                .ForMember(d => d.IsDraft, d => d.MapFrom(s => s.Draft))
                .ForMember(d => d.IsLatest, d => d.MapFrom(s => s.Latest))
                .ForMember(d => d.IsPublished, d => d.MapFrom(s => s.Published))
                .ForMember(d => d.IsLatestOrPublished, d => d.MapFrom(s => s.LatestOrPublished));
        }
    }
}