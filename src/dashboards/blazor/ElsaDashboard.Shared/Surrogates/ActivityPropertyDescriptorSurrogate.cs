using Elsa.Client.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using ProtoBuf;

namespace ElsaDashboard.Shared.Surrogates
{
    [ProtoContract(IgnoreListHandling = true)]
    public class ActivityPropertyDescriptorSurrogate
    {
        private static readonly JsonSerializerSettings SerializerSettings;

        static ActivityPropertyDescriptorSurrogate()
        {
            SerializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        public ActivityPropertyDescriptorSurrogate()
        {
        }

        public ActivityPropertyDescriptorSurrogate(ActivityPropertyInfo value)
        {
            Name = value.Name;
            Type = value.Type;
            Label = value.Label;
            Hint = value.Hint;
            Options = value.Options != null ? JsonConvert.SerializeObject(value.Options, SerializerSettings) : default;
        }

        [ProtoMember(1)] public string? Name { get; }
        [ProtoMember(2)] public string? Type { get; }
        [ProtoMember(3)] public string? Label { get; }
        [ProtoMember(4)] public string? Hint { get; }
        [ProtoMember(5)] public string? Options { get; }

        public static implicit operator ActivityPropertyInfo?(ActivityPropertyDescriptorSurrogate? surrogate) =>
            surrogate != null
                ? new ActivityPropertyInfo
                {
                    Name = surrogate.Name!,
                    Type = surrogate.Type!,
                    Hint = surrogate.Hint,
                    Label = surrogate.Label,
                    Options = !string.IsNullOrEmpty(surrogate.Options) ? JsonConvert.DeserializeObject<JObject>(surrogate.Options) : new JObject()
                }
                : default;

        public static implicit operator ActivityPropertyDescriptorSurrogate?(ActivityPropertyInfo? source) => source != null ? new ActivityPropertyDescriptorSurrogate(source) : default;
    }
}