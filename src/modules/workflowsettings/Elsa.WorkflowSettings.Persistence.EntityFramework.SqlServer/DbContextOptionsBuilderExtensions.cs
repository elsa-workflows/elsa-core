using Elsa.WorkflowSettings.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.SqlServer
{
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use SqlServer.
        /// </summary>
        public static DbContextOptionsBuilder UseWorkflowSettingsSqlServer(this DbContextOptionsBuilder builder, string connectionString) =>
            builder.UseSqlServer(connectionString, db => db
                .MigrationsAssembly(typeof(WorkflowSettingsSqlServerElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(WorkflowSettingsContext.MigrationsHistoryTable, WorkflowSettingsContext.ElsaSchema));
    }
}