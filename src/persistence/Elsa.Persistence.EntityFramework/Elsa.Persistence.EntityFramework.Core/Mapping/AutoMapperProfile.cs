using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Models;

namespace Elsa.Persistence.EntityFramework.Core.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<WorkflowInstance, WorkflowInstanceEntity>().ReverseMap();
            CreateMap<WorkflowDefinition, WorkflowDefinitionEntity>().ReverseMap();
            CreateMap<WorkflowExecutionLogRecord, WorkflowExecutionLogRecordEntity>().ReverseMap();
        }
    }
}