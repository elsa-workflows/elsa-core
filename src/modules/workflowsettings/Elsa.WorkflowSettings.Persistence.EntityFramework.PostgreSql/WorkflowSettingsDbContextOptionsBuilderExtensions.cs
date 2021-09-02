using Elsa.WorkflowSettings.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.PostgreSql
{
    public static class WorkflowSettingsDbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use PostgreSql.
        /// </summary>
        public static DbContextOptionsBuilder UseWorkflowSettingsPostgreSql(this DbContextOptionsBuilder builder, string connectionString) =>
            builder.UseNpgsql(connectionString, db => db
                .MigrationsAssembly(typeof(WorkflowSettingsPostgreSqlElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(WorkflowSettingsContext.MigrationsHistoryTable, WorkflowSettingsContext.ElsaSchema));
    }
}