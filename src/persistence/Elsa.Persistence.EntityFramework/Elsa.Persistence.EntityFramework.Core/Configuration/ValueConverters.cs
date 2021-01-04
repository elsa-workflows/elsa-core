using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;

namespace Elsa.Persistence.EntityFramework.Core.Configuration
{
    public static class ValueConverters
    {
        public static readonly ValueConverter<Instant, DateTimeOffset> InstantConverter = new(x => x.ToDateTimeOffset(), x => Instant.FromDateTimeOffset(x));
    }
}