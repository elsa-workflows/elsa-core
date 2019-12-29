using AutoMapper;
using Elsa.Models;

namespace Elsa.Mapping
{
    public class WorkflowDefinitionProfile : Profile
    {
        public WorkflowDefinitionProfile()
        {
            CreateMap<ProcessDefinitionVersion, ProcessDefinitionVersion>();
        }
    }
}