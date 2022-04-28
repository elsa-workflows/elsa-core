using Elsa.Services;
using RT.Comb;

namespace Elsa.Modules.Comb;

/// <summary>
/// This is the recommended technique for COMBs stored in Microsoft SQL Server.
/// </summary>
public class SqlIdentityGenerator : IIdentityGenerator
{
    public string GenerateId() => Provider.Sql.Create().ToString("N");
}