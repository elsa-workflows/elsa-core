using System;
using Elsa.AutoMapper.Extensions;
using Elsa.AutoMapper.Extensions.NodaTime;
using Elsa.Persistence.EntityFrameworkCore.Mapping;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions
{
    public static class EFCoreServiceCollectionExtensions
    {
        public static EntityFrameworkCoreElsaBuilder AddEntityFrameworkCoreProvider(
            this ElsaBuilder configuration,
            Action<DbContextOptionsBuilder> configureOptions,
            bool usePooling = true)
        {
            var services = configuration.Services;
            if (usePooling)
                services.AddDbContextPool<ElsaContext>(configureOptions);
            else
                services.AddDbContext<ElsaContext>(configureOptions);

            services
                .AddAutoMapperProfile<InstantProfile>(ServiceLifetime.Singleton)
                .AddAutoMapperProfile<DocumentProfile>(ServiceLifetime.Singleton);

            return new EntityFrameworkCoreElsaBuilder(configuration.Services);
        }

        public static EntityFrameworkCoreElsaBuilder AddEntityFrameworkStores(
            this ElsaBuilder configuration,
            Action<DbContextOptionsBuilder> configureOptions,
            bool usePooling = true)
        {
            return configuration
                .AddEntityFrameworkCoreProvider(configureOptions, usePooling)
                .AddWorkflowDefinitionStore()
                .AddWorkflowInstanceStore();
        }

        public static EntityFrameworkCoreElsaBuilder AddWorkflowInstanceStore(
            this EntityFrameworkCoreElsaBuilder configuration)
        {
            configuration.Services
                .AddScoped<IWorkflowInstanceStore, EntityFrameworkCoreWorkflowInstanceStore>();

            return configuration;
        }

        public static EntityFrameworkCoreElsaBuilder AddWorkflowDefinitionStore(
            this EntityFrameworkCoreElsaBuilder configuration)
        {
            configuration.Services
                .AddScoped<IWorkflowDefinitionStore, EntityFrameworkCoreWorkflowDefinitionStore>();

            return configuration;
        }
    }
}