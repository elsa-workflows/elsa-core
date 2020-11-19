using Elsa.Client.Models;
using ProtoBuf;

namespace ElsaDashboard.Shared.Surrogates
{
    [ProtoContract(IgnoreListHandling = true)]
    public class VersionOptionsSurrogate
    {
        public VersionOptionsSurrogate()
        {
        }

        public VersionOptionsSurrogate(VersionOptions value)
        {
            Value = value.ToString();
        }

        [ProtoMember(1)] public string Value { get; } = default!;
        
        public static implicit operator VersionOptions(VersionOptionsSurrogate surrogate) => VersionOptions.FromString(surrogate.Value);
        public static implicit operator VersionOptionsSurrogate(VersionOptions source) => new(source);
    }
}