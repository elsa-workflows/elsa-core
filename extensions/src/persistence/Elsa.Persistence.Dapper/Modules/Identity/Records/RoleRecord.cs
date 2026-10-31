using Elsa.Persistence.Dapper.Records;

namespace Elsa.Persistence.Dapper.Modules.Identity.Records;

internal class RoleRecord : Record
{
    public string Name { get; set; } = null!;
    public string Permissions { get; set; } = null!;
}