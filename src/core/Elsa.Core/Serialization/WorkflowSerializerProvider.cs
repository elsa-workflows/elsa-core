using System;
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
        private readonly TypeNameHandlingConverter typeNameHandlingConverter;

        public WorkflowSerializerProvider(TypeNameHandlingConverter typeNameHandlingConverter)
        {
            this.typeNameHandlingConverter = typeNameHandlingConverter;
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
                .WithConverter(typeNameHandlingConverter);
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
            jsonSerializer.Converters.Add(typeNameHandlingConverter);
            return jsonSerializer;
        }
    }
}