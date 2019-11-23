using AutoMapper;
using Elsa.Models;
using Elsa.Server.GraphQL.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.Server.GraphQL.Mapping
{
    public class GraphQLProfile : Profile
    {
        public GraphQLProfile()
        {
            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersionModel>().ReverseMap();
            
            CreateMap<ActivityDefinition, ActivityDefinitionModel>()
                .ForMember(d => d.State, d => d.MapFrom((s, _) => s.State.ToString()));

            CreateMap<ActivityDefinitionModel, ActivityDefinition>()
                .ForMember(d => d.State, d => d.MapFrom((s, _) => JObject.Parse(s.State ?? "{}")));

            CreateMap<DefineWorkflowDefinitionInputModel, WorkflowDefinitionVersion>();
            
            CreateMap<VersionOptionsModel, VersionOptions>()
                .ForMember(d => d.IsDraft, d => d.MapFrom(s => s.Draft))
                .ForMember(d => d.IsLatest, d => d.MapFrom(s => s.Latest))
                .ForMember(d => d.IsPublished, d => d.MapFrom(s => s.Published))
                .ForMember(d => d.IsLatestOrPublished, d => d.MapFrom(s => s.LatestOrPublished));
        }
    }
}