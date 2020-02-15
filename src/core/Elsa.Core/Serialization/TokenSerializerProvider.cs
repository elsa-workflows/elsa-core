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
        private readonly VariableConverter variableConverter;
        private readonly TypeConverter typeConverter;

        public TokenSerializerProvider(VariableConverter variableConverter, TypeConverter typeConverter)
        {
            this.variableConverter = variableConverter;
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
                .WithConverter(variableConverter)
                .WithConverter(typeConverter);
        }

        public JsonSerializer CreateJsonSerializer()
        {
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            jsonSerializer.ContractResolver = new CamelCasePropertyNamesContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false
                }
            };
            jsonSerializer.Converters.Add(variableConverter);
            jsonSerializer.Converters.Add(typeConverter);
            return jsonSerializer;
        }
    }
}