using System;
using AutoMapper;
using NodaTime;

namespace Elsa.Mapping
{
    public class NodaTimeProfile : Profile,
        ITypeConverter<Instant, DateTime>,
        ITypeConverter<Instant?, DateTime?>,
        ITypeConverter<DateTime, Instant>,
        ITypeConverter<DateTime?, Instant?>
    {
        public NodaTimeProfile()
        {
            CreateMap<Instant, DateTime>().ConvertUsing(this);
            CreateMap<Instant?, DateTime?>().ConvertUsing(this);
            CreateMap<DateTime, Instant>().ConvertUsing(this);
            CreateMap<DateTime?, Instant?>().ConvertUsing(this);
        }

        public DateTime Convert(Instant source, DateTime target, ResolutionContext context) => source.ToDateTimeUtc();
        public DateTime? Convert(Instant? source, DateTime? target, ResolutionContext context) => source?.ToDateTimeUtc();
        public Instant Convert(DateTime source, Instant target, ResolutionContext context) => Convert(source);
        public Instant? Convert(DateTime? source, Instant? target, ResolutionContext context) => source != null ? Convert(source.Value) : default(Instant?);

        public Instant Convert(DateTime source)
        {
            var utcDateTime = source.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(source, DateTimeKind.Utc)
                : source.ToUniversalTime();
            
            return Instant.FromDateTimeUtc(utcDateTime);
        }
    }
}