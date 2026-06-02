using Elsa.ModularPersistence.Runtime;

namespace Elsa.ModularPersistence.Endpoints.RuntimeEntities;

public sealed record RuntimeEntityRecordResponse(RuntimeEntityRecord? Item);

public sealed record RuntimeEntityRecordsResponse(IReadOnlyCollection<RuntimeEntityRecord> Items);

public sealed class DeleteRuntimeEntityRequest
{
    public long? ExpectedVersion { get; set; }

    public string? ProviderName { get; set; }
}

public sealed class RuntimeEntityEndpointRequest
{
    public string Id { get; set; } = default!;

    public string Data { get; set; } = default!;

    public string? TenantId { get; set; }

    public long? ExpectedVersion { get; set; }

    public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    public string? ProviderName { get; set; }
}

public sealed class RuntimeEntityQueryEndpointRequest
{
    public ICollection<RuntimeEntityQueryFilter> Filters { get; set; } = [];

    public int? Limit { get; set; }

    public int Offset { get; set; }

    public string? TenantId { get; set; }

    public string? ProviderName { get; set; }
}
