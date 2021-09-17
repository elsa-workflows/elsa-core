using Elsa.WorkflowSettings.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.MySql
{
    public static class WorkflowSettingsDbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use MySql 
        /// </summary>
        public static DbContextOptionsBuilder UseWorkflowSettingsMySql(this DbContextOptionsBuilder builder, string connectionString) =>
            builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), db => db
                .MigrationsAssembly(typeof(WorkflowSettingsMySqlElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(WorkflowSettingsContext.MigrationsHistoryTable, WorkflowSettingsContext.ElsaSchema)
                .SchemaBehavior(MySqlSchemaBehavior.Ignore));
    }
}