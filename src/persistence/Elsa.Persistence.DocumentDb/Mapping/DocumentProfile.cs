using Elsa.Models;
using Elsa.Persistence.DocumentDb.Documents;
using Elsa.Services;

namespace Elsa.Persistence.DocumentDb.Mapping
{
    public sealed class DocumentProfile : MappingProfile
    {
        private readonly ITenantProvider tenantProvider;

        public DocumentProfile(ITenantProvider tenantProvider)
        {
            this.tenantProvider = tenantProvider;

            CreateMap<WorkflowDefinitionVersion, WorkflowDefinitionVersionDocument>()
                .ForMember(d => d.TenantId, opt => opt.MapFrom(s => this.tenantProvider.GetTenantId<WorkflowDefinitionVersionDocument>()))
                .ReverseMap();
            CreateMap<WorkflowInstance, WorkflowInstanceDocument>()
                .ForMember(d => d.TenantId, opt => opt.MapFrom(s => this.tenantProvider.GetTenantId<WorkflowInstanceDocument>()))
                .ReverseMap();
        }
    }
}
