using Elsa.Dapper.Records;

namespace Elsa.Dapper.Modules.Identity.Records;

internal class RoleRecord : Record
{
    public string Name { get; set; } = default!;
    public string Permissions { get; set; } = default!;
}