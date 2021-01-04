using System;
using System.Data;
using System.Linq;
using Elsa.Data;
using Elsa.Persistence.EntityFramework.Core.Mapping;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Microsoft.AspNetCore.Http;
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
                .AddAutoMapperProfile<AutoMapperProfile>()
                .AddDbContext<ElsaContext>(configure);

            return elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<EntityFrameworkWorkflowExecutionLogRecordStore>());
        }
    }
}