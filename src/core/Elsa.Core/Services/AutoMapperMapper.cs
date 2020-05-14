using System.Collections.Generic;
using AutoMapper;

namespace Elsa.Services
{
    public class AutoMapperMapper : IMapper
    {
        private readonly AutoMapper.IMapper mapper;

        public AutoMapperMapper(IEnumerable<MapperProfile> profiles)
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