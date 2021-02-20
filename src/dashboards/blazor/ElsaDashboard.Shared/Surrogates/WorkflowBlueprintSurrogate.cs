using Elsa.Client.Converters;
using Elsa.Client.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using ProtoBuf;

namespace ElsaDashboard.Shared.Surrogates
{
    [ProtoContract(IgnoreListHandling = true)]
    public class WorkflowBlueprintSurrogate
    {
        private static readonly JsonSerializerSettings SerializerSettings;

        static WorkflowBlueprintSurrogate()
        {
            SerializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };
            
            SerializerSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            SerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
            SerializerSettings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
            SerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
            SerializerSettings.Converters.Add(new FlagEnumConverter(new DefaultNamingStrategy()));
        }

        public WorkflowBlueprintSurrogate(WorkflowBlueprint value)
        {
            Value = Serialize(value);
        }

        [ProtoMember(1)] public string Value { get; }

        public static implicit operator WorkflowBlueprint(WorkflowBlueprintSurrogate surrogate) => Deserialize(surrogate.Value)!;
        public static implicit operator WorkflowBlueprintSurrogate(WorkflowBlueprint source) => new(source);

        private static WorkflowBlueprint Deserialize(string json) => JsonConvert.DeserializeObject<WorkflowBlueprint>(json, SerializerSettings)!;
        private static string Serialize(WorkflowBlueprint value) => JsonConvert.SerializeObject(value, SerializerSettings)!;
    }
}