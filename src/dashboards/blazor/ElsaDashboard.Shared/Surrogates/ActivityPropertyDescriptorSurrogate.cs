using Elsa.Client.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;

namespace ElsaDashboard.Shared.Surrogates
{
    [ProtoContract(IgnoreListHandling = true)]
    public class ActivityPropertyDescriptorSurrogate
    {
        public ActivityPropertyDescriptorSurrogate(ActivityPropertyDescriptor value)
        {
            Name = value.Name;
            Type = value.UIHint;
            Label = value.Label;
            Hint = value.Hint;
            Options = value.Options?.ToString(Formatting.None);
            Category = value.Category;
            DefaultValue = value.DefaultValue?.ToString(Formatting.None);
        }

        [ProtoMember(1)] public string? Name { get; }
        [ProtoMember(2)] public string? Type { get; }
        [ProtoMember(3)] public string? Label { get; }
        [ProtoMember(4)] public string? Hint { get; }
        [ProtoMember(5)] public string? Options { get; }
        [ProtoMember(6)] public string? Category { get; }
        [ProtoMember(7)] public string? DefaultValue { get; }

        public static implicit operator ActivityPropertyDescriptor?(ActivityPropertyDescriptorSurrogate? surrogate) =>
            surrogate != null
                ? new ActivityPropertyDescriptor
                {
                    Name = surrogate.Name!,
                    UIHint = surrogate.Type!,
                    Hint = surrogate.Hint,
                    Label = surrogate.Label,
                    Options = surrogate.Options is null or "" ? default : JToken.Parse(surrogate.Options),
                    Category = surrogate.Category,
                    DefaultValue = surrogate.DefaultValue is null or "" ? default : JToken.Parse(surrogate.DefaultValue)
                }
                : default;

        public static implicit operator ActivityPropertyDescriptorSurrogate?(ActivityPropertyDescriptor? source) => source != null ? new ActivityPropertyDescriptorSurrogate(source) : default;
    }
}