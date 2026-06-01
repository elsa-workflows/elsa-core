namespace Elsa.ModularPersistence.Documents;

/// <summary>
/// Identifies one document within a provider.
/// </summary>
public sealed record DocumentKey
{
    public DocumentKey(string id, string type, string? tenantId = null)
    {
        Id = DocumentValidation.RequireText(id, nameof(id));
        Type = DocumentValidation.RequireText(type, nameof(type));
        TenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim();
    }

    public string Id { get; }

    public string Type { get; }

    public string? TenantId { get; }
}
