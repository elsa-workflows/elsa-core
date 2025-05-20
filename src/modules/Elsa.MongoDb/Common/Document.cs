namespace Elsa.MongoDb.Common;

public abstract class Document
{
    public string Id { get; set; } = default!;
    public string? TenantId { get; set; }
}