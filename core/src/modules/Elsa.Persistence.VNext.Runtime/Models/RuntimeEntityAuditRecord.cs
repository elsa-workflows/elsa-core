namespace Elsa.Persistence.VNext.Runtime.Models;

public class RuntimeEntityAuditRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SubjectType { get; set; } = default!;
    public string SubjectId { get; set; } = default!;
    public string Action { get; set; } = default!;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? Message { get; set; }
}
