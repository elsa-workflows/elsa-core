using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFrameworkCore.Documents;

namespace Elsa.Persistence.EntityFrameworkCore.Mapping
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