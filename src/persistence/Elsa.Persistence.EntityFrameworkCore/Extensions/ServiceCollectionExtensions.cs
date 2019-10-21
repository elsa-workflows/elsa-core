using System;
using Elsa.AutoMapper.Extensions;
using Elsa.AutoMapper.Extensions.NodaTime;
using Elsa.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Mapping;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static EntityFrameworkCoreServiceConfiguration WithEntityFrameworkCoreProvider(
            this ServiceConfiguration configuration,
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

            return new EntityFrameworkCoreServiceConfiguration(configuration.Services);
        }

        public static EntityFrameworkCoreServiceConfiguration WithWorkflowInstanceStore(
            this EntityFrameworkCoreServiceConfiguration configuration)
        {
            configuration.Services
                .AddScoped<IWorkflowInstanceStore, EntityFrameworkCoreWorkflowInstanceStore>();

            return configuration;
        }

        public static EntityFrameworkCoreServiceConfiguration WithWorkflowDefinitionStore(
            this EntityFrameworkCoreServiceConfiguration configuration)
        {
            configuration.Services
                .AddScoped<IWorkflowDefinitionStore, EntityFrameworkCoreWorkflowDefinitionStore>();

            return configuration;
        }
    }
}