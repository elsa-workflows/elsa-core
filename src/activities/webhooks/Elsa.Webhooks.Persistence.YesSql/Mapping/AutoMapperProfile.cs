using Elsa.Webhooks.Persistence.YesSql.Documents;
using Elsa.Webhooks.Models;
using AutoMapper;

namespace Elsa.Webhooks.Persistence.YesSql.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<WebhookDefinition, WebhookDefinitionDocument>()
                .ForMember(d => d.DefinitionId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.Id, d => d.Ignore())
                .ReverseMap()
                .ForMember(d => d.Id, d => d.MapFrom(s => s.DefinitionId));
        }
    }
}
