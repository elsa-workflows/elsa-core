using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using Elsa.Services;

namespace Elsa.Persistence.YesSql.Mapping
{
    public class DocumentProfile : MappingProfile
    {
        public DocumentProfile()
        {
            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersionDocument>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ForMember(d => d.WorkflowDefinitionVersionId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.WorkflowDefinitionId, d => d.MapFrom(s => s.DefinitionId))
                .ReverseMap()
                .AfterMap((s, d) => d.Id = s.WorkflowDefinitionVersionId)
                .AfterMap((s, d) => d.DefinitionId = s.WorkflowDefinitionId);

            CreateMap<WorkflowInstance, WorkflowInstanceDocument>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ForMember(d => d.WorkflowInstanceId, d => d.MapFrom(s => s.Id))
                .ReverseMap()
                .AfterMap((s, d) => d.Id = s.WorkflowInstanceId);;
        }
    }
}