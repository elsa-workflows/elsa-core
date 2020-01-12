using Elsa.Converters;
using Elsa.Serialization.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Serialization
{
    public class WorkflowSerializerProvider : IWorkflowSerializerProvider
    {
        private readonly VariableConverter variableConverter;
        private readonly ActivityConverter activityConverter;
        private readonly TypeConverter typeConverter;

        public WorkflowSerializerProvider(VariableConverter variableConverter, ActivityConverter activityConverter, TypeConverter typeConverter)
        {
            this.variableConverter = variableConverter;
            this.activityConverter = activityConverter;
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
                .WithConverter(activityConverter)
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
            jsonSerializer.Converters.Add(activityConverter);
            jsonSerializer.Converters.Add(typeConverter);
            return jsonSerializer;
        }
    }
}