using System;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkCore(this IServiceCollection services, Action<DbContextOptionsBuilder> configureOptions, bool usePooling = true)
        {
            if (usePooling)
                services.AddDbContextPool<ElsaContext>(configureOptions);
            else
                services.AddDbContext<ElsaContext>(configureOptions);

            return services;
        }
        
        public static IServiceCollection AddEntityFrameworkCoreWorkflowInstanceStore(this IServiceCollection services)
        {
            return services
                .AddScoped<IWorkflowInstanceStore, EntityFrameworkCoreWorkflowInstanceStore>();
        }

        public static IServiceCollection AddEntityFrameworkCoreWorkflowDefinitionStore(this IServiceCollection services)
        {
            return services
                .AddScoped<IWorkflowDefinitionStore, EntityFrameworkCoreWorkflowDefinitionStore>();
        }
    }
}