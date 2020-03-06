using AutoMapper;
using Elsa.Models;

namespace Elsa.Mapping
{
    public class WorkflowDefinitionVersionProfile : Profile
    {
        public WorkflowDefinitionVersionProfile()
        {
            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersion>();
        }
    }
}