using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Elsa.Server.Api.Extensions
{
    public static class JsonMvcBuilderExtensions
    {
        public static IMvcBuilder AddJsonSerialization(this IMvcBuilder builder) =>
            builder.AddJsonOptions(
                options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
                });
    }
}