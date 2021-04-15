using System;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace Elsa.Persistence.EntityFramework.Core.Configuration
{
    public static class ValueConverters
    {
        public static readonly ValueConverter<Instant, DateTimeOffset> InstantConverter = new(x => x.ToDateTimeOffset(), x => Instant.FromDateTimeOffset(x));
        public static readonly ValueConverter<Instant, DateTime> SqliteInstantConverter = new(x => x.ToDateTimeUtc(), x => Instant.FromDateTimeUtc(x.WithKind(DateTimeKind.Utc)));
        public static readonly ValueConverter<JObject?, string> JObjectConverter = new(x => x!.ToString(), x => JObject.Parse(x));
    }
}