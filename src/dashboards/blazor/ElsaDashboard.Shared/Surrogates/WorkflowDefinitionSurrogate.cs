using Elsa.Client.Models;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using ProtoBuf;

namespace ElsaDashboard.Shared.Surrogates
{
    [ProtoContract(IgnoreListHandling = true)]
    public class WorkflowDefinitionSurrogate
    {
        private static readonly JsonSerializerSettings SerializerSettings;
        
        static WorkflowDefinitionSurrogate()
        {
            SerializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public WorkflowDefinitionSurrogate(WorkflowDefinition value)
        {
            Value = Serialize(value);
        }

        [ProtoMember(1)] public string Value { get; }
        
        public static implicit operator WorkflowDefinition(WorkflowDefinitionSurrogate surrogate) => Deserialize(surrogate.Value)!;
        public static implicit operator WorkflowDefinitionSurrogate(WorkflowDefinition source) => new(source);

        private static WorkflowDefinition Deserialize(string json) => JsonConvert.DeserializeObject<WorkflowDefinition>(json, SerializerSettings)!;
        private static string Serialize(WorkflowDefinition value) => JsonConvert.SerializeObject(value, SerializerSettings)!;
    }
}