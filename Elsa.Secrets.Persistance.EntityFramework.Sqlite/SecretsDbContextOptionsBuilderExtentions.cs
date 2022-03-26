using Elsa.Secrets.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Persistence.EntityFramework.Sqlite
{
    public static class SecretsDbContextOptionsBuilderExtentions
    {
        public static DbContextOptionsBuilder UseWorkflowSettingsSqlite(this DbContextOptionsBuilder builder, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;") => builder.UseSqlite(connectionString, db => db
        .MigrationsAssembly(typeof(SecretsSqlietContextFactory).Assembly.GetName().Name)
        .MigrationsHistoryTable(SecretsContext.MigrationsHistoryTable, SecretsContext.ElsaSchema));
    }
}
