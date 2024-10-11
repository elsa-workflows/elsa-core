using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.SqlServer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EntityFrameworkCore.SqlServer;

[UsedImplicitly]
public class SqlServerSecretsDbContextFactory : SqlServerDesignTimeDbContextFactory<SecretsDbContext>
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<SecretsDbContext> builder, string connectionString)
    {
        builder.UseElsaSqlServer(GetType().Assembly, connectionString);
    }
}