using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;

namespace Elsa.Persistence.DocumentDb.Mapping
{
    public class DocumentProfile : Profile
    {
        public DocumentProfile()
        {
            CreateMap<ProcessDefinitionVersion, WorkflowDefinitionVersionDocument>().ReverseMap();
            CreateMap<ProcessInstance, WorkflowInstanceDocument>().ReverseMap();
        }
    }
}
