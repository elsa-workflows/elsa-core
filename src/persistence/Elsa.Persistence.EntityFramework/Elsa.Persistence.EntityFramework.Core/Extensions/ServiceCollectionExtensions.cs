using System;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFramework.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions UseEntityFrameworkPersistence(this ElsaOptions elsa, Action<DbContextOptionsBuilder> configure) => elsa.UseEntityFrameworkPersistence((_, builder) => configure(builder));

        public static ElsaOptions UseEntityFrameworkPersistence(this ElsaOptions elsa, Action<IServiceProvider, DbContextOptionsBuilder> configure)
        {
            elsa.Services
                .AddDbContext<ElsaContext>(configure)
                .AddScoped<EntityFrameworkWorkflowDefinitionStore>()
                .AddScoped<EntityFrameworkWorkflowInstanceStore>()
                .AddScoped<EntityFrameworkWorkflowExecutionLogRecordStore>()
                ;

            return elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowExecutionLogRecordStore>());
        }
    }
}