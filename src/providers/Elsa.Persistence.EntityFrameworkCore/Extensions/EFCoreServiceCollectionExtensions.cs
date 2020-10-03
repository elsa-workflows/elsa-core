using System;
using Elsa.Extensions;
using Elsa.Mapping;
using Elsa.Persistence.EntityFrameworkCore.CustomSchema;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Elsa.Persistence.EntityFrameworkCore.Mapping;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions
{
    public static class EFCoreServiceCollectionExtensions
    {
        public static ElsaOptions UseEntityFrameworkWorkflowStores(
            this ElsaOptions options,
            Action<DbContextOptionsBuilder> configureOptions,
            bool usePooling = true,
            string? schema = default, 
            string? migrationHistoryTableName = default)
        {
            return options
                .UseEntityFrameworkWorkflowDefinitionStore(configureOptions, usePooling, schema, migrationHistoryTableName)
                .UseEntityFrameworkWorkflowInstanceStore(configureOptions, usePooling);
        }
        
        public static ElsaOptions UseEntityFrameworkWorkflowInstanceStore(
            this ElsaOptions options,
            Action<DbContextOptionsBuilder> configureOptions,
            bool usePooling = true,
            string? schema = default, 
            string? migrationHistoryTableName = default)
        {
            options.AddEntityFrameworkCoreProvider(configureOptions, usePooling, schema, migrationHistoryTableName);
            options.Services.AddScoped<EntityFrameworkCoreWorkflowInstanceStore>();
            options.UseWorkflowInstanceStore(sp => sp.GetRequiredService<EntityFrameworkCoreWorkflowInstanceStore>());

            return options;
        }

        public static ElsaOptions UseEntityFrameworkWorkflowDefinitionStore(
            this ElsaOptions options,
            Action<DbContextOptionsBuilder> configureOptions,
            bool usePooling = true,
            string? schema = default, 
            string? migrationHistoryTableName = default)
        {
            options.AddEntityFrameworkCoreProvider(configureOptions, usePooling, schema, migrationHistoryTableName);
            options.Services.AddScoped<EntityFrameworkCoreWorkflowDefinitionStore>();
            options.UseWorkflowDefinitionStore(sp => sp.GetRequiredService<EntityFrameworkCoreWorkflowDefinitionStore>());

            return options;
        }
        
        private static ElsaOptions AddEntityFrameworkCoreProvider(
            this ElsaOptions options,
            Action<DbContextOptionsBuilder> configureOptions,
            bool usePooling = true,
            string? schema = default, 
            string? migrationHistoryTableName = default)
        {
            var services = options.Services;

            if (services.HasService<ElsaContext>())
                return options;
            
            if (usePooling)
                services.AddDbContextPool<ElsaContext>(configureOptions);
            else
                services.AddDbContext<ElsaContext>(configureOptions);

            services
                .AddAutoMapperProfile<NodaTimeProfile>(ServiceLifetime.Singleton)
                .AddAutoMapperProfile<EntitiesProfile>(ServiceLifetime.Singleton);
            
            if (!string.IsNullOrWhiteSpace(schema))
            {
                var historyTableName = !string.IsNullOrWhiteSpace(migrationHistoryTableName) 
                    ? migrationHistoryTableName 
                    : DbContextCustomSchema.DefaultMigrationsHistoryTableName;
                
                var dbContextCustomSchema = new DbContextCustomSchema(schema, historyTableName);

                services.AddSingleton<IDbContextCustomSchema>(dbContextCustomSchema);    
            }
            
            return options;
        }
    }
}