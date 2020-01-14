using System;
using Elsa.AutoMapper.Extensions;
using Elsa.AutoMapper.Extensions.NodaTime;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Elsa.Persistence.EntityFrameworkCore.Mapping;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions
{
    public static class EFCoreServiceCollectionExtensions
    {
        public static EntityFrameworkCoreElsaOptions AddEntityFrameworkCoreProvider(
            this ElsaOptions configuration,
            Action<DbContextOptionsBuilder> configureOptions,
            bool usePooling = true)
        {
            var services = configuration.Services;
            if (usePooling)
                services.AddDbContextPool<ElsaContext>(configureOptions);
            else
                services.AddDbContext<ElsaContext>(configureOptions);

            services
                .AddAutoMapperProfile<NodaTimeProfile>(ServiceLifetime.Singleton)
                .AddAutoMapperProfile<EntitiesProfile>(ServiceLifetime.Singleton);

            return new EntityFrameworkCoreElsaOptions(configuration.Services);
        }

        public static EntityFrameworkCoreElsaOptions AddEntityFrameworkStores(
            this ElsaOptions configuration,
            Action<DbContextOptionsBuilder> configureOptions,
            bool usePooling = true)
        {
            return configuration
                .AddEntityFrameworkCoreProvider(configureOptions, usePooling)
                .AddWorkflowDefinitionStore()
                .AddWorkflowInstanceStore();
        }

        public static EntityFrameworkCoreElsaOptions AddWorkflowInstanceStore(this EntityFrameworkCoreElsaOptions configuration)
        {
            configuration.Services
                .AddScoped<IWorkflowInstanceStore, EntityFrameworkCoreWorkflowInstanceStore>();

            return configuration;
        }

        public static EntityFrameworkCoreElsaOptions AddWorkflowDefinitionStore(this EntityFrameworkCoreElsaOptions configuration)
        {
            configuration.Services
                .AddScoped<IWorkflowDefinitionStore, EntityFrameworkCoreWorkflowDefinitionStore>();

            return configuration;
        }
    }
}