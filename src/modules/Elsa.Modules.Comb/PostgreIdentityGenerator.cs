using Elsa.Services;
using RT.Comb;

namespace Elsa.Modules.Comb;

/// <summary>
/// This is the recommended technique for COMBs stored in PostgreSQL
/// </summary>
public class PostgreIdentityGenerator : IIdentityGenerator
{
    public string GenerateId() => Provider.PostgreSql.Create().ToString("N");
}