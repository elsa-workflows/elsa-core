using System;
using System.Linq;
using Elsa.WorkflowSettings.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.SqlServer
{
    class WorkflowSettingsSqlServerElsaContextFactory : IDesignTimeDbContextFactory<WorkflowSettingsContext>
    {
        public WorkflowSettingsContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<WorkflowSettingsContext>();
            var connectionString = args.Any() ? args[0] : throw new InvalidOperationException("Please specify a connection string. E.g. dotnet ef database update -- \"Server=Local;Database=elsa\"");
            builder.UseSqlServer(connectionString, db => db
                .MigrationsAssembly(typeof(WorkflowSettingsSqlServerElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(WorkflowSettingsContext.MigrationsHistoryTable, WorkflowSettingsContext.ElsaSchema));
            return new WorkflowSettingsContext(builder.Options);
        }
    }
}