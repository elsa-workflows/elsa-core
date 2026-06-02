namespace Elsa.ModularPersistence.Documents;

/// <summary>
/// Represents the portable document shape persisted by document providers.
/// </summary>
public sealed record DocumentEnvelope
{
    public DocumentEnvelope(
        string id,
        string type,
        string? tenantId,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string data,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (version < 0)
            throw new ArgumentOutOfRangeException(nameof(version), "Document version cannot be negative.");

        if (updatedAt < createdAt)
            throw new ArgumentException("Updated timestamp cannot be earlier than created timestamp.", nameof(updatedAt));

        Id = DocumentValidation.RequireText(id, nameof(id));
        Type = DocumentValidation.RequireText(type, nameof(type));
        TenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim();
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Data = DocumentValidation.RequireText(data, nameof(data));
        Metadata = new Dictionary<string, string>(metadata ?? new Dictionary<string, string>(), StringComparer.Ordinal);
    }

    public string Id { get; }

    public string Type { get; }

    public string? TenantId { get; }

    public long Version { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public string Data { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    public DocumentKey Key => new(Id, Type, TenantId);
}
