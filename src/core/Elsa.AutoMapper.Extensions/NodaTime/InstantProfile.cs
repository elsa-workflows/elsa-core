using System;
using AutoMapper;
using NodaTime;

namespace Elsa.AutoMapper.Extensions.NodaTime
{
    public class InstantProfile : Profile,
        ITypeConverter<Instant, DateTime>,
        ITypeConverter<Instant?, DateTime?>,
        ITypeConverter<DateTime, Instant>,
        ITypeConverter<DateTime?, Instant?>
    {
        public InstantProfile()
        {
            CreateMap<Instant, DateTime>().ConvertUsing(this);
            CreateMap<Instant?, DateTime?>().ConvertUsing(this);
            CreateMap<DateTime, Instant>().ConvertUsing(this);
            CreateMap<DateTime?, Instant?>().ConvertUsing(this);
        }


        public DateTime Convert(Instant source, DateTime destination, ResolutionContext context)
        {
            return source.ToDateTimeUtc();
        }

        public DateTime? Convert(Instant? source, DateTime? destination, ResolutionContext context)
        {
            return source?.ToDateTimeUtc();
        }

        public Instant Convert(DateTime source, Instant destination, ResolutionContext context)
        {
            return Convert(source);
        }

        public Instant? Convert(DateTime? source, Instant? destination, ResolutionContext context)
        {
            return source != null ? Convert(source.Value) : default;
        }
        
        public Instant Convert(DateTime source)
        {
            var utcDateTime = source.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(source, DateTimeKind.Utc)
                : source.ToUniversalTime();
            
            return Instant.FromDateTimeUtc(utcDateTime);
        }
    }
}