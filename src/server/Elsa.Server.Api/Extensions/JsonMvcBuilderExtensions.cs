using System;
using Elsa.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Elsa.Server.Api.Extensions
{
    public static class JsonMvcBuilderExtensions
    {
        public static IMvcBuilder AddJsonSerialization(this IMvcBuilder builder, Action<JsonSerializerSettings>? configureOptions = default) =>
            builder.AddNewtonsoftJson(
                options =>
                {
                    configureOptions ??= DefaultContentSerializer.ConfigureDefaultJsonSerializationSettings;
                    configureOptions(options.SerializerSettings);
                });
    }
}