using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;

namespace Elsa.Persistence.DocumentDb.Mapping
{
    public class DocumentProfile : Profile
    {
        public DocumentProfile()
        {
            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersionDocument>().ReverseMap();
            CreateMap<WorkflowInstance, WorkflowInstanceDocument>().ReverseMap();
        }
    }
}
