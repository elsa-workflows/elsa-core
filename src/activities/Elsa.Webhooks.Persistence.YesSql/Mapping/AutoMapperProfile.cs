using Elsa.Webhooks.Abstractions.Models;
using Elsa.Webhooks.Persistence.YesSql.Documents;
using AutoMapper;

namespace Elsa.Webhooks.Persistence.YesSql.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<WebhookDefinition, WebhookDefinitionDocument>()
                .ForMember(d => d.Id, d => d.Ignore())
                .ReverseMap()
                .ForMember(d => d.Id, d => d.MapFrom(s => s.Id));
        }
    }
}
