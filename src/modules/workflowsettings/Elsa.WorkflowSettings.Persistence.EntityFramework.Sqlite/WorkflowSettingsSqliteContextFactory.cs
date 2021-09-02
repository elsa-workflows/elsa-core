using System.Linq;
using Elsa.WorkflowSettings.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.Sqlite
{
    public class WorkflowSettingsSqliteContextFactory : IDesignTimeDbContextFactory<WorkflowSettingsContext>
    {
        public WorkflowSettingsContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<WorkflowSettingsContext>();
            var connectionString = args.Any() ? args[0] : "Data Source=elsa.db;Cache=Shared";

            builder.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(WorkflowSettingsSqliteContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(WorkflowSettingsContext.MigrationsHistoryTable, WorkflowSettingsContext.ElsaSchema));

            return new WorkflowSettingsContext(builder.Options);
        }
    }
}