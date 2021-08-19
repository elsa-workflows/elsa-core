using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Persistence.YesSql.Documents;
using AutoMapper;

namespace Elsa.WorkflowSettings.Persistence.YesSql.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<WorkflowSetting, WorkflowSettingDocument>()
                .ForMember(d => d.SettingId, d => d.MapFrom(s => s.Id))
                .ForMember(d => d.Id, d => d.Ignore())
                .ReverseMap()
                .ForMember(d => d.Id, d => d.MapFrom(s => s.SettingId));
        }
    }
}
