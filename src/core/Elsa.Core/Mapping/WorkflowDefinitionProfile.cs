using Elsa.Models;
using Elsa.Services;

namespace Elsa.Mapping
{
    public sealed class WorkflowDefinitionProfile : MappingProfile
    {
        public WorkflowDefinitionProfile()
        {
            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersion>();
        }
    }
}