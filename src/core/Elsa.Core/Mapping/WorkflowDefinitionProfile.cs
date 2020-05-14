using Elsa.Models;
using Elsa.Services;

namespace Elsa.Mapping
{
    public sealed class WorkflowDefinitionProfile : MapperProfile
    {
        public WorkflowDefinitionProfile()
        {
            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersion>();
        }
    }
}