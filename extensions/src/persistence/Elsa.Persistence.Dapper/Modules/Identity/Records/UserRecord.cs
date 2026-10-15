using Elsa.Persistence.Dapper.Records;

namespace Elsa.Persistence.Dapper.Modules.Identity.Records;

internal class UserRecord : Record
{
    public string Name { get; set; } = null!;
    public string HashedPassword { get; set; } = null!;
    public string HashedPasswordSalt { get; set; } = null!;
    public string Roles { get; set; } = null!;
}