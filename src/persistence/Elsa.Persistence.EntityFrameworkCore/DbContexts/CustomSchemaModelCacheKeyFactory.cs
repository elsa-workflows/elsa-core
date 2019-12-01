using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Persistence.EntityFrameworkCore.DbContexts
{
    public class CustomSchemaModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public CustomSchemaModelCacheKeyFactory()
        {
        }
        public object Create(DbContext context)
        {
            string schema = null;
            if(context is ElsaContext && ElsaContext.Services != null)
            {
                IDbContextCustomSchema dbContextCustomSchema = null;
                using(var scope = ElsaContext.Services.BuildServiceProvider().CreateScope())
                {
                    dbContextCustomSchema = scope.ServiceProvider.GetService<IDbContextCustomSchema>();
                }
                if (dbContextCustomSchema != null && dbContextCustomSchema.UseCustomSchema)
                {
                    schema = dbContextCustomSchema.CustomDefaultSchema;
                }
            }
            return new
            {
                Type = context.GetType(),
                Schema = schema
            };
        }
    }
}