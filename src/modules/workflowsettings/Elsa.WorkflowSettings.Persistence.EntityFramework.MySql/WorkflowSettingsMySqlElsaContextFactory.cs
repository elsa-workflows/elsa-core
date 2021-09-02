using System;
using System.Linq;
using Elsa.WorkflowSettings.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.MySql
{
    public class WorkflowSettingsMySqlElsaContextFactory : IDesignTimeDbContextFactory<WorkflowSettingsContext>
    {
        public WorkflowSettingsContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<WorkflowSettingsContext>();
            var connectionString = args.Any() ? args[0] : throw new InvalidOperationException("Please specify a connection string. E.g. dotnet ef database update -- \"Server=localhost;Port=3306;Database=elsa;User=root;Password=password\"");
            var serverVersion = args.Length >= 2 ? args[1] : null;

            builder.UseMySql(
                connectionString,
                serverVersion != null ? ServerVersion.Parse(serverVersion) : ServerVersion.AutoDetect(connectionString),
                db => db
                    .MigrationsAssembly(typeof(WorkflowSettingsMySqlElsaContextFactory).Assembly.GetName().Name)
                    .MigrationsHistoryTable(WorkflowSettingsContext.MigrationsHistoryTable, WorkflowSettingsContext.ElsaSchema));

            return new WorkflowSettingsContext(builder.Options);
        }
    }
}