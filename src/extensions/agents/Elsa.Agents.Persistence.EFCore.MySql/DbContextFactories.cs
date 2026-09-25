using Elsa.Persistence.EFCore.Abstractions;
using Elsa.Persistence.EFCore.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Agents.Persistence.EFCore.MySql;

[UsedImplicitly]
public class MySqlAgentsDbContextFactory : DesignTimeDbContextFactoryBase<AgentsDbContext>
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<AgentsDbContext> builder, string connectionString)
    {
        builder.UseElsaMySql(GetType().Assembly, connectionString, serverVersion: MySqlServerVersion.LatestSupportedServerVersion);
    }
}