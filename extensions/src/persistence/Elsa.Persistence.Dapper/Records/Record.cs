namespace Elsa.Persistence.Dapper.Records;

public class Record
{
    public string Id { get; set; } = null!;
    public string? TenantId { get; set; }
}