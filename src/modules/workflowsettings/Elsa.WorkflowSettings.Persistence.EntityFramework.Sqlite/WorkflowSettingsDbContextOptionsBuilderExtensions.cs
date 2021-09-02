using Elsa.WorkflowSettings.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.Sqlite
{
    public static class WorkflowSettingsDbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseWorkflowSettingsSqlite(this DbContextOptionsBuilder builder, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;") => builder.UseSqlite(connectionString, db => db
            .MigrationsAssembly(typeof(WorkflowSettingsSqliteContextFactory).Assembly.GetName().Name)
            .MigrationsHistoryTable(WorkflowSettingsContext.MigrationsHistoryTable, WorkflowSettingsContext.ElsaSchema));
    }
}