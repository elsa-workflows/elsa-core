using System.Collections.Generic;
using AutoMapper;

namespace Elsa.Services
{
    /// <summary>
    /// A Mapper implementation that uses AutoMapper.
    /// Using a privately constructed AutoMapper.IMapper instance prevents collisions with other frameworks such as ABP.
    /// See https://github.com/elsa-workflows/elsa-core/issues/290 
    /// </summary>
    public class AutoMapperMapper : IMapper
    {
        private readonly AutoMapper.IMapper mapper;

        public AutoMapperMapper(IEnumerable<MappingProfile> profiles)
        {
            var configuration = new MapperConfiguration(
                x =>
                {
                    foreach (var profile in profiles)
                        x.AddProfile(profile);
                }
            );

            mapper = configuration.CreateMapper();
        }

        public TDestination Map<TSource, TDestination>(TSource source) => mapper.Map<TSource, TDestination>(source);
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination) => mapper.Map(source, destination);
        public TDestination Map<TDestination>(object source) => mapper.Map<TDestination>(source);
    }
}