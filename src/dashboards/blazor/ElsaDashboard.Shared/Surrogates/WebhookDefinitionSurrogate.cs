using Elsa.Client.Webhooks.Models;
using ProtoBuf;

namespace ElsaDashboard.Shared.Surrogates
{
    [ProtoContract(IgnoreListHandling = true)]
    public class WebhookDefinitionSurrogate
    {
        public WebhookDefinitionSurrogate(WebhookDefinition value)
        {
            Id = value.Id;
            TenantId = value.TenantId;
            Name = value.Name;
            Path = value.Path;
            Description = value.Description;
            PayloadTypeName = value.PayloadTypeName;
        }

        [ProtoMember(1)] public string? Id { get; }
        [ProtoMember(2)] public string? TenantId { get; }
        [ProtoMember(3)] public string? Name { get; }
        [ProtoMember(4)] public string? Path { get; }
        [ProtoMember(5)] public string? Description { get; }
        [ProtoMember(6)] public string? PayloadTypeName { get; }

        public static implicit operator WebhookDefinition?(WebhookDefinitionSurrogate? surrogate) =>
            surrogate != null
                ? new WebhookDefinition
                {
                    Id = surrogate.Id ?? string.Empty,
                    TenantId = surrogate.TenantId,
                    Name = surrogate.Name ?? string.Empty,
                    Path = surrogate.Path ?? string.Empty,
                    Description = surrogate.Description,
                    PayloadTypeName = surrogate.PayloadTypeName,
                }
                : default;

        public static implicit operator WebhookDefinitionSurrogate?(WebhookDefinition? source) =>
            source != null ? new WebhookDefinitionSurrogate(source) : default;
    }
}