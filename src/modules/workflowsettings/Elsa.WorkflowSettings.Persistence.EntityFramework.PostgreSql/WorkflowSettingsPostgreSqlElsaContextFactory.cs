using System.Linq;
using Elsa.WorkflowSettings.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.PostgreSql
{
    public class WorkflowSettingsPostgreSqlElsaContextFactory : IDesignTimeDbContextFactory<WorkflowSettingsContext>
    {
        public WorkflowSettingsContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<WorkflowSettingsContext>();
            var connectionString = args.Any() ? args[0] : "Server=127.0.0.1;Port=5432;Database=elsa;User Id=postgres;Password=password;";

            builder.UseNpgsql(
                connectionString,
                db => db.MigrationsAssembly(typeof(WorkflowSettingsPostgreSqlElsaContextFactory).Assembly.GetName().Name)
                    .MigrationsHistoryTable(WorkflowSettingsContext.MigrationsHistoryTable, WorkflowSettingsContext.ElsaSchema));

            return new WorkflowSettingsContext(builder.Options);
        }
    }
}