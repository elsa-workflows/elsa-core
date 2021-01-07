using System;
using Elsa.Persistence.EntityFramework.Core.StartupTasks;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFramework.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions UseEntityFrameworkPersistence(this ElsaOptions elsa, Action<DbContextOptionsBuilder> configure, bool autoRunMigrations = false) =>
            elsa.UseEntityFrameworkPersistence((_, builder) => configure(builder), autoRunMigrations);

        public static ElsaOptions UseEntityFrameworkPersistence(this ElsaOptions elsa, Action<IServiceProvider, DbContextOptionsBuilder> configure, bool autorunMigrations = false)
        {
            elsa.Services
                .AddDbContext<ElsaContext>(configure)
                .AddScoped<EntityFrameworkWorkflowDefinitionStore>()
                .AddScoped<EntityFrameworkWorkflowInstanceStore>()
                .AddScoped<EntityFrameworkWorkflowExecutionLogRecordStore>();

            if (autorunMigrations)
                elsa.Services.AddStartupTask<RunEFCoreMigrations>();

            return elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowExecutionLogRecordStore>());
        }
    }
}