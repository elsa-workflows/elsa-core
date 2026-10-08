namespace Elsa.Persistence.MongoDb.Common;

public abstract class Document
{
    public string Id { get; set; } = null!;
    public string? TenantId { get; set; }
}