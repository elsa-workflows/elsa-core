using Elsa.Client.Models;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using ProtoBuf;

namespace ElsaDashboard.Shared.Surrogates
{
    [ProtoContract(IgnoreListHandling = true)]
    public class WorkflowInstanceSurrogate
    {
        private static readonly JsonSerializerSettings SerializerSettings;

        static WorkflowInstanceSurrogate()
        {
            SerializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            }.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public WorkflowInstanceSurrogate(WorkflowInstance value)
        {
            Value = Serialize(value);
        }

        [ProtoMember(1)] public string Value { get; }

        public static implicit operator WorkflowInstance(WorkflowInstanceSurrogate surrogate) => Deserialize(surrogate.Value)!;
        public static implicit operator WorkflowInstanceSurrogate(WorkflowInstance source) => new(source);

        private static WorkflowInstance Deserialize(string json) => JsonConvert.DeserializeObject<WorkflowInstance>(json, SerializerSettings)!;
        private static string Serialize(WorkflowInstance value) => JsonConvert.SerializeObject(value, SerializerSettings)!;
    }
}