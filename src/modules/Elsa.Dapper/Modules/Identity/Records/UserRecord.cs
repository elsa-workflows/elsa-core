using Elsa.Dapper.Records;

namespace Elsa.Dapper.Modules.Identity.Records;

internal class UserRecord : Record
{
    public string Name { get; set; } = default!;
    public string HashedPassword { get; set; } = default!;
    public string HashedPasswordSalt { get; set; } = default!;
    public string Roles { get; set; } = default!;
}