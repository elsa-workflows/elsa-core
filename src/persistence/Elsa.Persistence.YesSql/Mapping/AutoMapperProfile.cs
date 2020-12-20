using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;

namespace Elsa.Persistence.YesSql.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<WorkflowInstance, WorkflowInstanceDocument>().ReverseMap();
            CreateMap<WorkflowDefinition, WorkflowDefinitionDocument>().ReverseMap();
        }
    }
}