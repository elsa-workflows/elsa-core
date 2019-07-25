using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;

namespace Elsa.Persistence.YesSql.Mapping
{
    public class DocumentProfile : Profile
    {
        public DocumentProfile()
        {
            CreateMap<WorkflowDefinition, WorkflowDefinitionDocument>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ForMember(d => d.WorkflowDefinitionId, d => d.MapFrom(s => s.Id))
                .ReverseMap();
            
            CreateMap<WorkflowInstance, WorkflowInstanceDocument>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ForMember(d => d.WorkflowInstanceId, d => d.MapFrom(s => s.Id))
                .ReverseMap();
        }
    }
}