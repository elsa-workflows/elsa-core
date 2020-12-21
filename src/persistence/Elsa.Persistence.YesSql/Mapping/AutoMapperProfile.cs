using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;

namespace Elsa.Persistence.YesSql.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<WorkflowInstance, WorkflowInstanceDocument>()
                .ForMember(d => d.InstanceId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.Id, d => d.Ignore())
                .ReverseMap()
                .ForMember(d => d.Id, d => d.MapFrom(s => s.InstanceId));
            
            CreateMap<WorkflowDefinition, WorkflowDefinitionDocument>()
                .ForMember(d => d.DefinitionId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.Id, d => d.Ignore())
                .ReverseMap()
                .ForMember(d => d.Id, d => d.MapFrom(s => s.DefinitionId));
            
            CreateMap<WorkflowExecutionLogRecord, WorkflowExecutionLogRecordDocument>()
                .ForMember(d => d.RecordId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.Id, d => d.Ignore())
                .ReverseMap()
                .ForMember(d => d.Id, d => d.MapFrom(s => s.RecordId));
        }
    }
}