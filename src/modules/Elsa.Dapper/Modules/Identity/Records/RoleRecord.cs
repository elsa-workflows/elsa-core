namespace Elsa.Dapper.Modules.Identity.Records;

internal class RoleRecord
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Permissions { get; set; } = default!;
}