using AutoMapper;
using Elsa.Models;

namespace Elsa.Mapping
{
    /// <summary>
    /// An AutoMapper profile that configures certain domain models to be mappable from and to itself, effectively creating deep clones.
    /// </summary>
    public class CloningProfile : Profile
    {
        public CloningProfile()
        {
            CreateMap<WorkflowDefinition, WorkflowDefinition>();
            CreateMap<WorkflowInstance, WorkflowInstance>();
        }
    }
}