using Elsa.Converters;
using Elsa.Serialization.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Serialization
{
    public class TokenSerializerProvider : ITokenSerializerProvider
    {
        private readonly TypeConverter typeConverter;

        public TokenSerializerProvider(TypeConverter typeConverter)
        {
            this.typeConverter = typeConverter;
        }

        public JsonSerializerSettings CreateJsonSerializerSettings()
        {
            return new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new CamelCasePropertyNamesContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy
                        {
                            ProcessDictionaryKeys = false
                        }
                    },
                }
                .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)
                .WithConverter(typeConverter);
        }

        public JsonSerializer CreateJsonSerializer()
        {
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            jsonSerializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            jsonSerializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            jsonSerializer.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
            jsonSerializer.TypeNameHandling = TypeNameHandling.Objects;
            jsonSerializer.ContractResolver = new CamelCasePropertyNamesContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false
                }
            };
            jsonSerializer.Converters.Add(typeConverter);
            return jsonSerializer;
        }
    }
}