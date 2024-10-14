using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.PostgreSql;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EntityFrameworkCore.PostgreSql;

[UsedImplicitly]
public class PostgreSqlSecretsDbContextFactory : PostgreSqlDesignTimeDbContextFactory<SecretsDbContext>
{
    protected override void ConfigureBuilder(DbContextOptionsBuilder<SecretsDbContext> builder, string connectionString)
    {
        builder.UseElsaPostgreSql(GetType().Assembly, connectionString);
    }
}