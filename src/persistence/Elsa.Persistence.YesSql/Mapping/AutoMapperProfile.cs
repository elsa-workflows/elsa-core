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
                .ForMember(d => d.DefinitionVersionId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.Id, d => d.Ignore())
                .ReverseMap()
                .ForMember(d => d.Id, d => d.MapFrom(s => s.DefinitionVersionId));

            CreateMap<WorkflowExecutionLogRecord, WorkflowExecutionLogRecordDocument>()
                .ForMember(d => d.RecordId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.Id, d => d.Ignore())
                .ReverseMap()
                .ForMember(d => d.Id, d => d.MapFrom(s => s.RecordId));

            CreateMap<Bookmark, BookmarkDocument>()
                .ForMember(d => d.BookmarkId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.Id, d => d.Ignore())
                .ReverseMap()
                .ForMember(d => d.Id, d => d.MapFrom(s => s.BookmarkId));

            CreateMap<Trigger, TriggerDocument>()
                .ForMember(d => d.TriggerId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.Id, d => d.Ignore())
                .ReverseMap()
                .ForMember(d => d.Id, d => d.MapFrom(s => s.TriggerId));
        }
    }
}