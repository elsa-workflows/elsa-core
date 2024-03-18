namespace Elsa.Dapper.Records;

internal class Record
{
    public string Id { get; set; } = default!;
    public string? TenantId { get; set; }
}