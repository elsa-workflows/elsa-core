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
        public static EntityFrameworkCoreElsaBuilder AddEntityFrameworkCoreProvider<TElsaContext>(
            this ElsaBuilder configuration,
            Action<DbContextOptionsBuilder> configureOptions,
            bool usePooling = true)
            where TElsaContext : ElsaContext
        {
            var services = configuration.Services;
            if (usePooling)
                services.AddDbContextPool<ElsaContext, TElsaContext>(configureOptions);
            else
                services.AddDbContext<ElsaContext, TElsaContext>(configureOptions);

            services
                .AddAutoMapperProfile<NodaTimeProfile>(ServiceLifetime.Singleton)
                .AddAutoMapperProfile<EntitiesProfile>(ServiceLifetime.Singleton);

            return new EntityFrameworkCoreElsaBuilder(configuration.Services);
        }

        public static EntityFrameworkCoreElsaBuilder AddEntityFrameworkStores<TElsaContext>(
            this ElsaBuilder configuration,
            Action<DbContextOptionsBuilder> configureOptions,
            bool usePooling = true)
            where TElsaContext : ElsaContext
        {
            return configuration
                .AddEntityFrameworkCoreProvider<TElsaContext>(configureOptions, usePooling)
                .AddWorkflowDefinitionStore()
                .AddWorkflowInstanceStore();
        }

        public static EntityFrameworkCoreElsaBuilder AddWorkflowInstanceStore(this EntityFrameworkCoreElsaBuilder configuration)
        {
            configuration.Services
                .AddScoped<IWorkflowInstanceStore, EntityFrameworkCoreWorkflowInstanceStore>();

            return configuration;
        }

        public static EntityFrameworkCoreElsaBuilder AddWorkflowDefinitionStore(this EntityFrameworkCoreElsaBuilder configuration)
        {
            configuration.Services
                .AddScoped<IWorkflowDefinitionStore, EntityFrameworkCoreWorkflowDefinitionStore>();

            return configuration;
        }
    }
}