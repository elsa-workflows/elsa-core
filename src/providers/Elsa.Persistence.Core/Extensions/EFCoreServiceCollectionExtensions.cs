using System;
using Elsa.Extensions;
using Elsa.Mapping;
using Elsa.Persistence.Core.DbContexts;
using Elsa.Persistence.Core.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Core.Extensions
{
    public static class EFCoreServiceCollectionExtensions
    {
        public static ElsaOptions UsePersistence(
            this ElsaOptions options,
            Action<DbContextOptionsBuilder> configureOptions,
            bool usePooling = true)
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
            
            return options;
        }
    }
}